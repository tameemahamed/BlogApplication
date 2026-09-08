using Abp.Authorization;
using Abp.Runtime.Session;
using Abp.UI;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.Comments;
using BlogApplication.Comments.Dto;
using BlogApplication.Posts;
using BlogApplication.Posts.Dto;
using BlogApplication.Upvotes;
using BlogApplication.Upvotes.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BlogApplication.Tests.Upvotes;

public class UpvoteAppService_Tests : BlogApplicationTestBase
{
    private readonly IUpvoteAppService _upvoteAppService;
    private readonly IPostAppService _postAppService;
    private readonly ICommentAppService _commentAppService;

    public UpvoteAppService_Tests()
    {
        _upvoteAppService = Resolve<IUpvoteAppService>();
        _postAppService = Resolve<IPostAppService>();
        _commentAppService = Resolve<ICommentAppService>();
    }

    private async Task<User> CreateUserAsync(string userName, string roleName = null)
    {
        var user = new User
        {
            TenantId = null,
            UserName = userName,
            Name = userName,
            Surname = "Test",
            EmailAddress = userName + "@blogapplication.com",
            IsActive = true,
            IsEmailConfirmed = true
        };

        user.SetNormalizedNames();

        var userManager = Resolve<UserManager>();
        await userManager.InitializeOptionsAsync(null);
        (await userManager.CreateAsync(user, "123qwe")).Succeeded.ShouldBeTrue();

        if (roleName != null)
        {
            (await userManager.AddToRoleAsync(user, roleName)).Succeeded.ShouldBeTrue();
        }

        return user;
    }

    private async Task<PostDto> CreateApprovedPostAsync(string title, string authorUserName = "post.author")
    {
        await CreateUserAsync(authorUserName, StaticRoleNames.Host.Author);
        LoginAsHost(authorUserName);

        var post = await _postAppService.CreateAsync(new CreatePostInput
        {
            Title = title,
            ContentMarkdown = "# " + title
        });
        await _postAppService.SubmitAsync(post.Id);

        LoginAsHostAdmin();
        await _postAppService.ApproveAsync(post.Id);

        return post;
    }

    private Task<CommentDto> CommentAsync(Guid postId, string content)
    {
        return _commentAppService.CreateCommentAsync(new CreateCommentInput
        {
            PostId = postId,
            ContentMarkdown = content
        });
    }

    [Fact]
    public async Task Should_Toggle_Upvote_On_And_Off()
    {
        var post = await CreateApprovedPostAsync("Toggle");
        var input = new ToggleUpvoteInput { TargetType = (int)UpvoteTargetType.Post, TargetId = post.Id };

        var output = await _upvoteAppService.ToggleAsync(input);
        output.Upvoted.ShouldBeTrue();
        output.UpvoteCount.ShouldBe(1);

        // toggle off: physically deleted, slot freed (prd.md D4)
        output = await _upvoteAppService.ToggleAsync(input);
        output.Upvoted.ShouldBeFalse();
        output.UpvoteCount.ShouldBe(0);

        // re-upvote works on the freed slot
        output = await _upvoteAppService.ToggleAsync(input);
        output.Upvoted.ShouldBeTrue();
        output.UpvoteCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Count_Upvotes_From_Multiple_Users()
    {
        var post = await CreateApprovedPostAsync("Multi");
        var input = new ToggleUpvoteInput { TargetType = (int)UpvoteTargetType.Post, TargetId = post.Id };

        await _upvoteAppService.ToggleAsync(input);

        await CreateUserAsync("second.voter");
        LoginAsHost("second.voter");
        var output = await _upvoteAppService.ToggleAsync(input);

        output.UpvoteCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Not_Upvote_Non_Approved_Post()
    {
        await CreateUserAsync("draft.author", StaticRoleNames.Host.Author);
        LoginAsHost("draft.author");
        var draft = await _postAppService.CreateAsync(new CreatePostInput
        {
            Title = "Draft",
            ContentMarkdown = "d"
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _upvoteAppService.ToggleAsync(new ToggleUpvoteInput
            {
                TargetType = (int)UpvoteTargetType.Post,
                TargetId = draft.Id
            }));
    }

    [Fact]
    public async Task Should_Not_Upvote_Comment_When_Post_Is_Not_Approved()
    {
        var post = await CreateApprovedPostAsync("Pending again");
        await CreateUserAsync("commenter.ten");
        LoginAsHost("commenter.ten");
        var comment = await CommentAsync(post.Id, "c");

        // editing the approved post sends it back to review (prd.md A4)
        LoginAsHost("post.author");
        await _postAppService.UpdateAsync(new UpdatePostInput
        {
            Id = post.Id,
            Title = post.Title,
            ContentMarkdown = "v2"
        });

        LoginAsHost("commenter.ten");
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _upvoteAppService.ToggleAsync(new ToggleUpvoteInput
            {
                TargetType = (int)UpvoteTargetType.Comment,
                TargetId = comment.Id
            }));
    }

    [Fact]
    public async Task Should_Throw_For_Invalid_TargetType()
    {
        var post = await CreateApprovedPostAsync("Invalid");

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            _upvoteAppService.ToggleAsync(new ToggleUpvoteInput { TargetType = 99, TargetId = post.Id }));
    }

    [Fact]
    public async Task Upvote_Ban_Should_Block_Toggle()
    {
        var post = await CreateApprovedPostAsync("Banned toggle");
        await CreateUserAsync("banned.voter");
        LoginAsHost("banned.voter");

        // impose an upvote ban: user-level prohibition (prd.md E5-S4)
        var userManager = Resolve<UserManager>();
        var permissionManager = Resolve<Abp.Authorization.IPermissionManager>();
        var bannedUser = await GetCurrentUserAsync();
        await userManager.ProhibitPermissionAsync(
            bannedUser,
            permissionManager.GetPermission(PermissionNames.Blog.Upvotes_Toggle));

        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            _upvoteAppService.ToggleAsync(new ToggleUpvoteInput
            {
                TargetType = (int)UpvoteTargetType.Post,
                TargetId = post.Id
            }));
    }

    [Fact]
    public async Task Public_Posts_Should_Carry_Counts_And_Flags()
    {
        var post = await CreateApprovedPostAsync("Flagged");
        var input = new ToggleUpvoteInput { TargetType = (int)UpvoteTargetType.Post, TargetId = post.Id };
        await _upvoteAppService.ToggleAsync(input);

        // as the upvoting user
        var mine = await _postAppService.GetPublicPostsAsync(
            new GetPublicPostsInput { SkipCount = 0, MaxResultCount = 10 });
        mine.Items[0].UpvoteCount.ShouldBe(1);
        mine.Items[0].UpvotedByCurrentUser.ShouldBe(true);

        // as anonymous: count visible, flag absent (prd.md A1)
        AbpSession.UserId = null;
        var anonymous = await _postAppService.GetPublicPostsAsync(
            new GetPublicPostsInput { SkipCount = 0, MaxResultCount = 10 });
        anonymous.Items[0].UpvoteCount.ShouldBe(1);
        anonymous.Items[0].UpvotedByCurrentUser.ShouldBeNull();
    }

    [Fact]
    public async Task Public_Posts_Should_Sort_By_Top_When_Requested()
    {
        // "More" is created (and published) BEFORE "Less" - so newest-first
        // and top-sort produce different orders. Distinct authors because
        // this fact calls the helper twice within its single database.
        var more = await CreateApprovedPostAsync("More", "top.author.one");
        await _upvoteAppService.ToggleAsync(new ToggleUpvoteInput
        {
            TargetType = (int)UpvoteTargetType.Post,
            TargetId = more.Id
        });
        var less = await CreateApprovedPostAsync("Less", "top.author.two");

        var top = await _postAppService.GetPublicPostsAsync(
            new GetPublicPostsInput { SkipCount = 0, MaxResultCount = 10, SortByUpvotes = true });
        top.Items[0].Title.ShouldBe("More");
        top.Items[1].Title.ShouldBe("Less");

        var newest = await _postAppService.GetPublicPostsAsync(
            new GetPublicPostsInput { SkipCount = 0, MaxResultCount = 10 });
        newest.Items[0].Title.ShouldBe("Less");
        newest.Items[1].Title.ShouldBe("More");
    }

    [Fact]
    public async Task Comment_Thread_Should_Carry_Counts_And_Flags()
    {
        var post = await CreateApprovedPostAsync("Thread votes");
        await CreateUserAsync("commenter.eleven");
        LoginAsHost("commenter.eleven");
        var comment = await CommentAsync(post.Id, "Voted comment");

        await _upvoteAppService.ToggleAsync(new ToggleUpvoteInput
        {
            TargetType = (int)UpvoteTargetType.Comment,
            TargetId = comment.Id
        });

        var thread = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        thread.Comments[0].UpvoteCount.ShouldBe(1);
        thread.Comments[0].UpvotedByCurrentUser.ShouldBe(true);
    }

    [Fact]
    public async Task Should_Keep_Single_Upvote_Row_Per_User_And_Target()
    {
        var post = await CreateApprovedPostAsync("Unique row");
        var input = new ToggleUpvoteInput { TargetType = (int)UpvoteTargetType.Post, TargetId = post.Id };
        var userId = AbpSession.GetUserId();

        await _upvoteAppService.ToggleAsync(input);
        await _upvoteAppService.ToggleAsync(input);
        await _upvoteAppService.ToggleAsync(input);

        await UsingDbContextAsync(async context =>
        {
            var rows = await context.Upvotes
                .Where(u => u.UserId == userId &&
                            u.TargetType == (int)UpvoteTargetType.Post &&
                            u.TargetId == post.Id)
                .ToListAsync();
            rows.Count.ShouldBe(1);
        });
    }
}
