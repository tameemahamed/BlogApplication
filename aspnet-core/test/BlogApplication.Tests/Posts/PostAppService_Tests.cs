using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Runtime.Session;
using Abp.Runtime.Validation;
using Abp.UI;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.Posts;
using BlogApplication.Posts.Dto;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace BlogApplication.Tests.Posts;

public class PostAppService_Tests : BlogApplicationTestBase
{
    private readonly IPostAppService _postAppService;

    public PostAppService_Tests()
    {
        _postAppService = Resolve<IPostAppService>();
    }

    private async Task<User> CreateAuthorAsync(string userName)
    {
        var user = new User
        {
            TenantId = null,
            UserName = userName,
            Name = userName,
            Surname = "Author",
            EmailAddress = userName + "@blogapplication.com",
            IsActive = true,
            IsEmailConfirmed = true
        };

        user.SetNormalizedNames();

        var userManager = Resolve<UserManager>();
        await userManager.InitializeOptionsAsync(null);
        (await userManager.CreateAsync(user, "123qwe")).Succeeded.ShouldBeTrue();
        (await userManager.AddToRoleAsync(user, StaticRoleNames.Host.Author)).Succeeded.ShouldBeTrue();
        return user;
    }

    private Task<PostDto> CreatePostAsync(string title)
    {
        return _postAppService.CreateAsync(new CreatePostInput
        {
            Title = title,
            ContentMarkdown = "# " + title
        });
    }

    [Fact]
    public async Task Should_Create_Post_As_Draft_With_Unique_Slug()
    {
        var post = await CreatePostAsync("Hello World");

        post.Status.ShouldBe((int)PostStatus.Draft);
        post.Slug.ShouldBe("hello-world");
        post.Title.ShouldBe("Hello World");
        post.AuthorId.ShouldBe(AbpSession.GetUserId());
        post.AuthorUserName.ShouldNotBeNullOrWhiteSpace();

        var second = await CreatePostAsync("Hello World");
        second.Slug.ShouldBe("hello-world-2");
    }

    [Fact]
    public async Task Should_Fallback_Slug_When_Title_Has_No_Usable_Characters()
    {
        var post = await CreatePostAsync("!!!");
        post.Slug.ShouldBe("post");
    }

    // prd.md E3-S4: oversized content is rejected server-side with a localized message
    [Fact]
    public async Task Should_Reject_Oversized_Content()
    {
        var input = new CreatePostInput
        {
            Title = "Oversized",
            ContentMarkdown = new string('x', PostConsts.MaxContentLength + 1)
        };

        await Should.ThrowAsync<AbpValidationException>(() => _postAppService.CreateAsync(input));
    }

    [Fact]
    public async Task Should_Walk_Through_Lifecycle()
    {
        var post = await CreatePostAsync("Lifecycle");

        // submit -> pending
        await _postAppService.SubmitAsync(post.Id);
        (await _postAppService.GetForEditAsync(post.Id)).Status.ShouldBe((int)PostStatus.PendingReview);

        // approve -> approved + published
        await _postAppService.ApproveAsync(post.Id);
        var approved = await _postAppService.GetForEditAsync(post.Id);
        approved.Status.ShouldBe((int)PostStatus.Approved);
        approved.PublishedAt.ShouldNotBeNull();

        // editing a published post sends it back to review (prd.md A4)
        await _postAppService.UpdateAsync(new UpdatePostInput
        {
            Id = post.Id,
            Title = "Lifecycle v2",
            ContentMarkdown = "v2"
        });
        var edited = await _postAppService.GetForEditAsync(post.Id);
        edited.Status.ShouldBe((int)PostStatus.PendingReview);
        edited.PublishedAt.ShouldBeNull();

        // reject with a reason
        await _postAppService.RejectAsync(new RejectPostInput { Id = post.Id, Reason = "Not good enough" });
        var rejected = await _postAppService.GetForEditAsync(post.Id);
        rejected.Status.ShouldBe((int)PostStatus.Rejected);
        rejected.RejectionReason.ShouldBe("Not good enough");

        // resubmit -> review again
        await _postAppService.SubmitAsync(post.Id);
        (await _postAppService.GetForEditAsync(post.Id)).Status.ShouldBe((int)PostStatus.PendingReview);

        // approve again, then archive
        await _postAppService.ApproveAsync(post.Id);
        await _postAppService.ArchiveAsync(post.Id);
        (await _postAppService.GetForEditAsync(post.Id)).Status.ShouldBe((int)PostStatus.Archived);

        // republish requires re-review
        await _postAppService.SubmitAsync(post.Id);
        (await _postAppService.GetForEditAsync(post.Id)).Status.ShouldBe((int)PostStatus.PendingReview);
    }

    [Fact]
    public async Task Should_Reject_Illegal_Transitions()
    {
        var post = await CreatePostAsync("Illegal");

        // draft: cannot be approved, rejected or archived
        await Should.ThrowAsync<UserFriendlyException>(() => _postAppService.ApproveAsync(post.Id));
        await Should.ThrowAsync<UserFriendlyException>(
            () => _postAppService.RejectAsync(new RejectPostInput { Id = post.Id, Reason = "x" }));
        await Should.ThrowAsync<UserFriendlyException>(() => _postAppService.ArchiveAsync(post.Id));

        // already pending: cannot be submitted twice
        await _postAppService.SubmitAsync(post.Id);
        await Should.ThrowAsync<UserFriendlyException>(() => _postAppService.SubmitAsync(post.Id));
    }

    [Fact]
    public async Task Author_Cannot_Modify_Or_Delete_Others_Posts()
    {
        await CreateAuthorAsync("author.one");
        await CreateAuthorAsync("author.two");

        LoginAsHost("author.one");
        var post = await CreatePostAsync("Author one post");

        LoginAsHost("author.two");
        await Should.ThrowAsync<AbpAuthorizationException>(
            () => _postAppService.UpdateAsync(new UpdatePostInput { Id = post.Id, Title = "Hijacked", ContentMarkdown = "Hijacked" }));
        await Should.ThrowAsync<AbpAuthorizationException>(() => _postAppService.DeleteAsync(post.Id));
        await Should.ThrowAsync<AbpAuthorizationException>(() => _postAppService.GetForEditAsync(post.Id));

        // moderators and admins may manage any post
        LoginAsHostAdmin();
        await _postAppService.UpdateAsync(new UpdatePostInput { Id = post.Id, Title = "Moderated", ContentMarkdown = "Moderated" });
    }

    [Fact]
    public async Task Author_Cannot_Delete_Own_Approved_Post()
    {
        await CreateAuthorAsync("author.approved");

        LoginAsHost("author.approved");
        var post = await CreatePostAsync("Approved post");
        await _postAppService.SubmitAsync(post.Id);

        LoginAsHostAdmin();
        await _postAppService.ApproveAsync(post.Id);

        LoginAsHost("author.approved");
        await Should.ThrowAsync<UserFriendlyException>(() => _postAppService.DeleteAsync(post.Id));

        // moderators and admins may delete any post (soft delete)
        LoginAsHostAdmin();
        await _postAppService.DeleteAsync(post.Id);
        await Should.ThrowAsync<EntityNotFoundException>(() => _postAppService.GetForEditAsync(post.Id));
    }

    [Fact]
    public async Task Should_Filter_My_Posts_And_Pending_Queue()
    {
        var author = await CreateAuthorAsync("author.filter");

        LoginAsHost("author.filter");
        await CreatePostAsync("One");
        var two = await CreatePostAsync("Two");
        await _postAppService.SubmitAsync(two.Id);

        var all = await _postAppService.GetMyPostsAsync(new GetMyPostsInput { MaxResultCount = 10 });
        all.TotalCount.ShouldBe(2);
        all.Items.ShouldContain(p => p.Status == (int)PostStatus.Draft);
        all.Items.ShouldContain(p => p.Status == (int)PostStatus.PendingReview);

        var drafts = await _postAppService.GetMyPostsAsync(
            new GetMyPostsInput { Status = (int)PostStatus.Draft, MaxResultCount = 10 });
        drafts.TotalCount.ShouldBe(1);
        drafts.Items[0].Title.ShouldBe("One");

        // the review queue lists pending posts only, oldest first
        var queue = await _postAppService.GetPendingReviewPostsAsync(new PagedResultRequestDto { SkipCount = 0, MaxResultCount = 10 });
        queue.TotalCount.ShouldBe(1);
        queue.Items[0].Title.ShouldBe("Two");
        queue.Items[0].AuthorUserName.ShouldBe(author.UserName);
    }

    [Fact]
    public async Task Public_Reads_Return_Approved_Only()
    {
        var draft = await CreatePostAsync("Draft post");
        var submitted = await CreatePostAsync("Submitted post");
        await _postAppService.SubmitAsync(submitted.Id);

        // anonymous request
        AbpSession.UserId = null;

        var publicPosts = await _postAppService.GetPublicPostsAsync(
            new PagedResultRequestDto { SkipCount = 0, MaxResultCount = 10 });
        publicPosts.TotalCount.ShouldBe(0);
        publicPosts.Items.ShouldBeEmpty();

        await Should.ThrowAsync<EntityNotFoundException>(() => _postAppService.GetPublicPostBySlugAsync(draft.Slug));
        await Should.ThrowAsync<EntityNotFoundException>(() => _postAppService.GetPublicPostBySlugAsync(submitted.Slug));

        // once approved, the post becomes publicly readable
        LoginAsHostAdmin();
        await _postAppService.ApproveAsync(submitted.Id);

        AbpSession.UserId = null;
        var after = await _postAppService.GetPublicPostsAsync(
            new PagedResultRequestDto { SkipCount = 0, MaxResultCount = 10 });
        after.TotalCount.ShouldBe(1);
        after.Items[0].Slug.ShouldBe(submitted.Slug);
        after.Items[0].AuthorUserName.ShouldNotBeNullOrWhiteSpace();

        var detail = await _postAppService.GetPublicPostBySlugAsync(submitted.Slug);
        detail.Title.ShouldBe("Submitted post");
        detail.ContentMarkdown.ShouldNotBeNullOrWhiteSpace();
    }
}
