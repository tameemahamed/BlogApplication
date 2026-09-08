using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.UI;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.Bans;
using BlogApplication.Bans.Dto;
using BlogApplication.Comments;
using BlogApplication.Comments.Dto;
using BlogApplication.Posts;
using BlogApplication.Posts.Dto;
using BlogApplication.Upvotes;
using BlogApplication.Upvotes.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BlogApplication.Tests.Bans;

public class UserBanAppService_Tests : BlogApplicationTestBase
{
    private readonly IUserBanAppService _userBanAppService;
    private readonly IPostAppService _postAppService;
    private readonly ICommentAppService _commentAppService;
    private readonly IUpvoteAppService _upvoteAppService;
    private readonly UserManager _userManager;

    public UserBanAppService_Tests()
    {
        _userBanAppService = Resolve<IUserBanAppService>();
        _postAppService = Resolve<IPostAppService>();
        _commentAppService = Resolve<ICommentAppService>();
        _upvoteAppService = Resolve<IUpvoteAppService>();
        _userManager = Resolve<UserManager>();
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

        await _userManager.InitializeOptionsAsync(null);
        (await _userManager.CreateAsync(user, "123qwe")).Succeeded.ShouldBeTrue();

        if (roleName != null)
        {
            (await _userManager.AddToRoleAsync(user, roleName)).Succeeded.ShouldBeTrue();
        }

        return user;
    }

    private async Task<PostDto> CreateApprovedPostAsync(string title, string authorUserName)
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

    private Task BanAsync(long userId, string reason, params BanType[] banTypes)
    {
        return _userBanAppService.BanAsync(new BanUserInput
        {
            UserId = userId,
            BanTypes = banTypes.Select(t => (int)t).ToList(),
            Reason = reason
        });
    }

    [Fact]
    public async Task Ban_Writes_Prohibition_And_Ledger_Row()
    {
        var bannedUser = await CreateUserAsync("banned.user");
        var moderator = await CreateUserAsync("ledger.moderator", StaticRoleNames.Host.Moderator);

        LoginAsHost("ledger.moderator");
        await BanAsync(bannedUser.Id, "spamming the thread", BanType.Comment);

        // enforcement side: user-level prohibition, checked by the framework
        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Comments_Create))
            .ShouldBeFalse();

        // ledger side: reason + actor recorded, ban active (prd.md E6-S5)
        await UsingDbContextAsync(async context =>
        {
            var row = await context.UserBans.SingleAsync(b => b.UserId == bannedUser.Id);
            row.BanType.ShouldBe((int)BanType.Comment);
            row.Reason.ShouldBe("spamming the thread");
            row.BannedByUserId.ShouldBe(moderator.Id);
            row.LiftedAt.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Banned_User_Cannot_Comment_Until_Unban()
    {
        var post = await CreateApprovedPostAsync("Ban target", "ban.target.author");
        var bannedUser = await CreateUserAsync("banned.commenter");

        LoginAsHost("banned.commenter");
        await _commentAppService.CreateCommentAsync(new CreateCommentInput
        {
            PostId = post.Id,
            ContentMarkdown = "before the ban"
        });

        LoginAsHostAdmin();
        await BanAsync(bannedUser.Id, "hostile", BanType.Comment);

        LoginAsHost("banned.commenter");
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            _commentAppService.CreateCommentAsync(new CreateCommentInput
            {
                PostId = post.Id,
                ContentMarkdown = "during the ban"
            }));

        LoginAsHostAdmin();
        await _userBanAppService.UnbanAsync(new UnbanInput
        {
            UserId = bannedUser.Id,
            BanType = (int)BanType.Comment
        });

        // restored via role-derived permissions - no forced grant (prd.md 4.1)
        LoginAsHost("banned.commenter");
        await _commentAppService.CreateCommentAsync(new CreateCommentInput
        {
            PostId = post.Id,
            ContentMarkdown = "after the unban"
        });

        await UsingDbContextAsync(async context =>
        {
            var settings = await context.Permissions
                .OfType<UserPermissionSetting>()
                .Where(p => p.UserId == bannedUser.Id)
                .ToListAsync();
            settings.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task Unban_Stamps_Ledger_And_Keeps_History()
    {
        var bannedUser = await CreateUserAsync("lifted.user");
        var unbanner = await CreateUserAsync("lift.moderator", StaticRoleNames.Host.Moderator);

        LoginAsHost("lift.moderator");
        await BanAsync(bannedUser.Id, "vote manipulation", BanType.Upvote);

        LoginAsHost("lift.moderator");
        await _userBanAppService.UnbanAsync(new UnbanInput
        {
            UserId = bannedUser.Id,
            BanType = (int)BanType.Upvote
        });

        await UsingDbContextAsync(async context =>
        {
            // history retained - rows are never deleted (prd.md E6-S5)
            var rows = await context.UserBans.Where(b => b.UserId == bannedUser.Id).ToListAsync();
            rows.Count.ShouldBe(1);
            rows[0].LiftedAt.ShouldNotBeNull();
            rows[0].LiftedByUserId.ShouldBe(unbanner.Id);
        });

        // capability restored immediately
        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Upvotes_Toggle))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Ban_Types_Are_Independent()
    {
        var post = await CreateApprovedPostAsync("Granular", "granular.author");
        var bannedUser = await CreateUserAsync("granular.commenter");

        LoginAsHost("granular.commenter");
        var comment = await _commentAppService.CreateCommentAsync(new CreateCommentInput
        {
            PostId = post.Id,
            ContentMarkdown = "root comment"
        });

        LoginAsHostAdmin();
        await BanAsync(bannedUser.Id, "hostile comments", BanType.Comment);

        // comment ban blocks top-level comments only (prd.md A6)
        LoginAsHost("granular.commenter");
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            _commentAppService.CreateCommentAsync(new CreateCommentInput
            {
                PostId = post.Id,
                ContentMarkdown = "blocked"
            }));

        // replies still work
        await _commentAppService.CreateReplyAsync(new CreateReplyInput
        {
            ParentCommentId = comment.Id,
            ContentMarkdown = "reply still allowed"
        });

        // upvoting still works
        await _upvoteAppService.ToggleAsync(new ToggleUpvoteInput
        {
            TargetType = (int)UpvoteTargetType.Post,
            TargetId = post.Id
        });
    }

    [Fact]
    public async Task Only_Bans_Manage_Can_Ban()
    {
        var target = await CreateUserAsync("ban.target");
        await CreateUserAsync("ban.author.user", StaticRoleNames.Host.Author);

        LoginAsHost("ban.author.user");
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            BanAsync(target.Id, "no authority", BanType.Comment));
    }

    [Fact]
    public async Task GetMyActiveBans_Returns_Type_And_Reason()
    {
        var bannedUser = await CreateUserAsync("restricted.user");

        LoginAsHostAdmin();
        await BanAsync(bannedUser.Id, "thread derailing", BanType.Reply);

        LoginAsHost("restricted.user");
        var bans = await _userBanAppService.GetMyActiveBansAsync();
        bans.Count.ShouldBe(1);
        bans[0].BanType.ShouldBe((int)BanType.Reply);
        bans[0].Reason.ShouldBe("thread derailing");

        LoginAsHostAdmin();
        await _userBanAppService.UnbanAsync(new UnbanInput
        {
            UserId = bannedUser.Id,
            BanType = (int)BanType.Reply
        });

        LoginAsHost("restricted.user");
        (await _userBanAppService.GetMyActiveBansAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Cannot_Ban_Self()
    {
        var moderator = await CreateUserAsync("self.ban.moderator", StaticRoleNames.Host.Moderator);

        LoginAsHost("self.ban.moderator");
        await Should.ThrowAsync<UserFriendlyException>(() =>
            BanAsync(moderator.Id, "self", BanType.Comment));
    }

    [Fact]
    public async Task Double_Ban_Throws()
    {
        var bannedUser = await CreateUserAsync("double.banned");

        LoginAsHostAdmin();
        await BanAsync(bannedUser.Id, "first", BanType.Comment);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            BanAsync(bannedUser.Id, "second", BanType.Comment));
    }

    [Fact]
    public async Task Ban_Multiple_Types_At_Once()
    {
        var post = await CreateApprovedPostAsync("Multi ban", "multi.ban.author");
        var bannedUser = await CreateUserAsync("multi.banned");

        LoginAsHost("multi.banned");
        var comment = await _commentAppService.CreateCommentAsync(new CreateCommentInput
        {
            PostId = post.Id,
            ContentMarkdown = "root"
        });

        LoginAsHostAdmin();
        await BanAsync(bannedUser.Id, "all abilities", BanType.Comment, BanType.Reply, BanType.Upvote);

        // every requested ability is denied by the framework
        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Comments_Create))
            .ShouldBeFalse();
        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Replies_Create))
            .ShouldBeFalse();
        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Upvotes_Toggle))
            .ShouldBeFalse();

        // one ledger row per banned type, sharing the action's reason
        await UsingDbContextAsync(async context =>
        {
            var rows = await context.UserBans
                .Where(b => b.UserId == bannedUser.Id && b.LiftedAt == null)
                .ToListAsync();
            rows.Count.ShouldBe(3);
            rows.Select(r => r.BanType).OrderBy(t => t).ShouldBe(new[]
            {
                (int)BanType.Comment, (int)BanType.Reply, (int)BanType.Upvote
            });
            rows.ShouldAllBe(r => r.Reason == "all abilities");
        });

        // and the user is blocked on all three write paths
        LoginAsHost("multi.banned");
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            _commentAppService.CreateCommentAsync(new CreateCommentInput
            {
                PostId = post.Id,
                ContentMarkdown = "blocked"
            }));
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            _commentAppService.CreateReplyAsync(new CreateReplyInput
            {
                ParentCommentId = comment.Id,
                ContentMarkdown = "blocked"
            }));
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            _upvoteAppService.ToggleAsync(new ToggleUpvoteInput
            {
                TargetType = (int)UpvoteTargetType.Post,
                TargetId = post.Id
            }));
    }

    [Fact]
    public async Task Unban_One_Type_Keeps_Others()
    {
        var bannedUser = await CreateUserAsync("partial.banned");

        LoginAsHostAdmin();
        await BanAsync(bannedUser.Id, "all abilities", BanType.Comment, BanType.Reply, BanType.Upvote);

        await _userBanAppService.UnbanAsync(new UnbanInput
        {
            UserId = bannedUser.Id,
            BanType = (int)BanType.Reply
        });

        // the lifted ability is restored, the others stay prohibited
        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Replies_Create))
            .ShouldBeTrue();
        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Comments_Create))
            .ShouldBeFalse();
        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Upvotes_Toggle))
            .ShouldBeFalse();

        LoginAsHost("partial.banned");
        var activeBans = await _userBanAppService.GetMyActiveBansAsync();
        activeBans.Count.ShouldBe(2);
        activeBans.ShouldNotContain(b => b.BanType == (int)BanType.Reply);
    }

    [Fact]
    public async Task Ban_Rejects_List_Containing_Active_Type()
    {
        var bannedUser = await CreateUserAsync("partial.reject");

        LoginAsHostAdmin();
        await BanAsync(bannedUser.Id, "first", BanType.Comment);

        // a stale client asking to re-ban an active type fails as a whole -
        // no partial application (prd.md E6-S1, one unit of work)
        await Should.ThrowAsync<UserFriendlyException>(() =>
            BanAsync(bannedUser.Id, "second", BanType.Comment, BanType.Reply));

        (await _userManager.IsGrantedAsync(bannedUser.Id, PermissionNames.Blog.Replies_Create))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task GetUserBans_Listing()
    {
        var first = await CreateUserAsync("listed.one");
        var second = await CreateUserAsync("listed.two");

        LoginAsHostAdmin();
        await BanAsync(first.Id, "r1", BanType.Comment);
        await BanAsync(second.Id, "r2", BanType.Upvote);

        // lifting drops the row out of the active list but keeps history
        await _userBanAppService.UnbanAsync(new UnbanInput
        {
            UserId = second.Id,
            BanType = (int)BanType.Upvote
        });

        var active = await _userBanAppService.GetUserBansAsync(
            new GetUserBansInput { OnlyActive = true, SkipCount = 0, MaxResultCount = 10 });
        active.TotalCount.ShouldBe(1);
        active.Items[0].UserId.ShouldBe(first.Id);
        active.Items[0].UserName.ShouldBe("listed.one");
        active.Items[0].BannedByUserName.ShouldBe(AbpUserBase.AdminUserName);

        var filtered = await _userBanAppService.GetUserBansAsync(
            new GetUserBansInput { UserId = second.Id, SkipCount = 0, MaxResultCount = 10 });
        filtered.TotalCount.ShouldBe(1);
        filtered.Items[0].LiftedAt.ShouldNotBeNull();
        filtered.Items[0].LiftedByUserName.ShouldBe(AbpUserBase.AdminUserName);
    }
}
