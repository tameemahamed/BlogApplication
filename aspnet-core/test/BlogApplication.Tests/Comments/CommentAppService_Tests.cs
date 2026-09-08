using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Runtime.Session;
using Abp.Runtime.Validation;
using Abp.UI;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.Comments;
using BlogApplication.Comments.Dto;
using BlogApplication.Posts;
using BlogApplication.Posts.Dto;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BlogApplication.Tests.Comments;

public class CommentAppService_Tests : BlogApplicationTestBase
{
    private readonly ICommentAppService _commentAppService;
    private readonly IPostAppService _postAppService;

    public CommentAppService_Tests()
    {
        _commentAppService = Resolve<ICommentAppService>();
        _postAppService = Resolve<IPostAppService>();
    }

    #region Helpers

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

    private async Task<PostDto> CreateApprovedPostAsync(string title)
    {
        await CreateUserAsync("post.author", StaticRoleNames.Host.Author);
        LoginAsHost("post.author");

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

    private async Task<CommentDto> CommentAsync(Guid postId, string content)
    {
        return await _commentAppService.CreateCommentAsync(new CreateCommentInput
        {
            PostId = postId,
            ContentMarkdown = content
        });
    }

    #endregion

    [Fact]
    public async Task Should_Create_Comment_On_Approved_Post()
    {
        var post = await CreateApprovedPostAsync("Commentable post");
        await CreateUserAsync("commenter.one");

        LoginAsHost("commenter.one");
        var comment = await CommentAsync(post.Id, "First comment");

        comment.PostId.ShouldBe(post.Id);
        comment.UserName.ShouldBe("commenter.one");
        comment.IsEdited.ShouldBeFalse();
        comment.ParentCommentId.ShouldBeNull();

        // publishes immediately (prd.md A2)
        var thread = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        thread.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Comment_On_Unapproved_Post()
    {
        await CreateUserAsync("draft.author", StaticRoleNames.Host.Author);
        LoginAsHost("draft.author");
        var draft = await _postAppService.CreateAsync(new CreatePostInput
        {
            Title = "Draft",
            ContentMarkdown = "draft"
        });

        await Should.ThrowAsync<UserFriendlyException>(() => CommentAsync(draft.Id, "nope"));

        // and the thread of an unapproved post is empty (no leaks)
        var thread = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = draft.Id, SkipCount = 0, MaxResultCount = 10 });
        thread.TotalCount.ShouldBe(0);
        thread.Comments.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reply_Should_Derive_Post_From_Parent()
    {
        var post = await CreateApprovedPostAsync("Reply target");
        await CreateUserAsync("commenter.two");

        LoginAsHost("commenter.two");
        var comment = await CommentAsync(post.Id, "Parent comment");
        var reply = await _commentAppService.CreateReplyAsync(new CreateReplyInput
        {
            ParentCommentId = comment.Id,
            ContentMarkdown = "A reply"
        });

        reply.PostId.ShouldBe(post.Id);
        reply.ParentCommentId.ShouldBe(comment.Id);

        // reply to a reply (prd.md A5)
        var replyToReply = await _commentAppService.CreateReplyAsync(new CreateReplyInput
        {
            ParentCommentId = reply.Id,
            ContentMarkdown = "Nested"
        });
        replyToReply.ParentCommentId.ShouldBe(reply.Id);

        var thread = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        thread.TotalCount.ShouldBe(3); // comment + reply + reply-to-reply
        thread.TopLevelCount.ShouldBe(1);
        thread.Comments[0].Replies.Count.ShouldBe(2); // flattened subtree
    }

    [Fact]
    public async Task Reply_To_Nonexistent_Comment_Should_Throw()
    {
        var post = await CreateApprovedPostAsync("Orphan reply target");

        await Should.ThrowAsync<Exception>(() =>
            _commentAppService.CreateReplyAsync(new CreateReplyInput
            {
                ParentCommentId = Guid.NewGuid(),
                ContentMarkdown = "orphan"
            }));
    }

    [Fact]
    public async Task Should_Edit_Own_Comment_Only()
    {
        var post = await CreateApprovedPostAsync("Editable");
        await CreateUserAsync("commenter.three");
        await CreateUserAsync("commenter.four");

        LoginAsHost("commenter.three");
        var comment = await CommentAsync(post.Id, "Original");

        await _commentAppService.UpdateCommentAsync(new UpdateCommentInput
        {
            Id = comment.Id,
            ContentMarkdown = "Edited"
        });

        var thread = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        thread.Comments[0].ContentMarkdown.ShouldBe("Edited");
        thread.Comments[0].IsEdited.ShouldBeTrue();

        LoginAsHost("commenter.four");
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            _commentAppService.UpdateCommentAsync(new UpdateCommentInput
            {
                Id = comment.Id,
                ContentMarkdown = "Hijacked"
            }));
    }

    [Fact]
    public async Task Delete_Authority_Should_Follow_A10()
    {
        var post = await CreateApprovedPostAsync("Deletable");
        await CreateUserAsync("commenter.five");

        LoginAsHost("commenter.five");
        var comment = await CommentAsync(post.Id, "To be moderated");

        // a random third user cannot delete someone else's comment
        await CreateUserAsync("bystander.user");
        LoginAsHost("bystander.user");
        await Should.ThrowAsync<AbpAuthorizationException>(() => _commentAppService.DeleteCommentAsync(comment.Id));

        // the post's author can (A10)
        LoginAsHost("post.author");
        await _commentAppService.DeleteCommentAsync(comment.Id);

        var thread = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        thread.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Admin_Can_Delete_Any_Comment()
    {
        var post = await CreateApprovedPostAsync("Admin moderated");
        await CreateUserAsync("commenter.six");

        LoginAsHost("commenter.six");
        var comment = await CommentAsync(post.Id, "Spam");

        LoginAsHostAdmin();
        await _commentAppService.DeleteCommentAsync(comment.Id);
    }

    [Fact]
    public async Task Deleting_Comment_Should_Hide_Reply_Subtree()
    {
        var post = await CreateApprovedPostAsync("Cascade");
        await CreateUserAsync("commenter.seven");

        LoginAsHost("commenter.seven");
        var root = await CommentAsync(post.Id, "Root");
        var reply = await _commentAppService.CreateReplyAsync(new CreateReplyInput
        {
            ParentCommentId = root.Id,
            ContentMarkdown = "Reply to root"
        });
        await _commentAppService.CreateReplyAsync(new CreateReplyInput
        {
            ParentCommentId = reply.Id,
            ContentMarkdown = "Reply to reply"
        });
        await CommentAsync(post.Id, "Unrelated top-level");

        var before = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        before.TotalCount.ShouldBe(4);
        before.TopLevelCount.ShouldBe(2);

        // deleting the root hides its whole subtree (prd.md E4-S6)
        await _commentAppService.DeleteCommentAsync(root.Id);

        var after = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        after.TotalCount.ShouldBe(1); // only the unrelated comment remains
        after.Comments.Single().ContentMarkdown.ShouldBe("Unrelated top-level");
    }

    [Fact]
    public async Task Thread_Should_Order_TopLevel_Newest_First()
    {
        var post = await CreateApprovedPostAsync("Ordering");
        await CreateUserAsync("commenter.eight");

        LoginAsHost("commenter.eight");
        await CommentAsync(post.Id, "First");
        await CommentAsync(post.Id, "Second");
        await CommentAsync(post.Id, "Third");

        var thread = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        thread.Comments.Select(c => c.ContentMarkdown).ShouldBe(new[] { "Third", "Second", "First" });

        // pagination applies to top-level comments
        var firstPage = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 2 });
        firstPage.Comments.Count.ShouldBe(2);
        firstPage.TopLevelCount.ShouldBe(3);
        firstPage.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Anonymous_User_Cannot_Comment_But_Can_Read()
    {
        var post = await CreateApprovedPostAsync("Public read");
        await CreateUserAsync("commenter.nine");
        LoginAsHost("commenter.nine");
        await CommentAsync(post.Id, "Readable");

        AbpSession.UserId = null;

        var thread = await _commentAppService.GetCommentThreadAsync(
            new GetCommentThreadInput { PostId = post.Id, SkipCount = 0, MaxResultCount = 10 });
        thread.TotalCount.ShouldBe(1);

        await Should.ThrowAsync<AbpAuthorizationException>(() => CommentAsync(post.Id, "anonymous"));
    }

    [Fact]
    public async Task Comment_Ban_Should_Block_Comment_But_Not_Reply()
    {
        var post = await CreateApprovedPostAsync("Ban target");
        await CreateUserAsync("banned.commenter");
        LoginAsHost("banned.commenter");
        var otherComment = await CommentAsync(post.Id, "Someone else's comment");

        // impose a comment ban: user-level prohibition (prd.md E4-S7)
        var userManager = Resolve<UserManager>();
        var permissionManager = Resolve<Abp.Authorization.IPermissionManager>();
        var bannedUser = await GetCurrentUserAsync();
        await userManager.ProhibitPermissionAsync(
            bannedUser,
            permissionManager.GetPermission(PermissionNames.Blog.Comments_Create));

        await Should.ThrowAsync<AbpAuthorizationException>(() => CommentAsync(post.Id, "blocked"));

        // granular: replying still works (separate permission)
        var reply = await _commentAppService.CreateReplyAsync(new CreateReplyInput
        {
            ParentCommentId = otherComment.Id,
            ContentMarkdown = "still allowed"
        });
        reply.ContentMarkdown.ShouldBe("still allowed");
    }

    [Fact]
    public async Task Should_Reject_Oversized_Comment_And_Reply()
    {
        var post = await CreateApprovedPostAsync("Length limits");
        await CreateUserAsync("commenter.ten");
        LoginAsHost("commenter.ten");

        var oversized = new string('x', CommentConsts.MaxContentLength + 1);

        await Should.ThrowAsync<AbpValidationException>(() => CommentAsync(post.Id, oversized));

        var comment = await CommentAsync(post.Id, "within the limit");
        await Should.ThrowAsync<AbpValidationException>(() => _commentAppService.CreateReplyAsync(
            new CreateReplyInput
            {
                ParentCommentId = comment.Id,
                ContentMarkdown = oversized
            }));
    }
}
