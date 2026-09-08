using Abp;
using Abp.Authorization.Users;
using Abp.Timing;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.Bans;
using BlogApplication.Comments;
using BlogApplication.Posts;
using BlogApplication.Upvotes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace BlogApplication.EntityFrameworkCore.Seed.Host;

/// <summary>
/// Demo content for a fresh environment (prd.md E8-S2): one user per role,
/// sample posts covering every workflow status, a comment thread with replies,
/// upvotes, and one banned user whose permission prohibition is seeded too.
/// Everything is idempotent - the Migrator may run many times, and each block
/// looks up its row before inserting. Content follows the design rules
/// (prd.md §6): plain Markdown, no emojis, no icons.
/// </summary>
public class BlogDemoDataCreator
{
    private const string DemoPassword = "123qwe";

    public const string WelcomeSlug = "welcome-to-the-blog";
    public const string MarkdownSlug = "writing-in-markdown";

    private readonly BlogApplicationDbContext _context;
    private readonly IGuidGenerator _guidGenerator;

    public BlogDemoDataCreator(BlogApplicationDbContext context, IGuidGenerator guidGenerator)
    {
        _context = context;
        _guidGenerator = guidGenerator;
    }

    public void Create()
    {
        var author = GetOrCreateUser("demo.author", "Demo", "Author", StaticRoleNames.Host.Author);
        var moderator = GetOrCreateUser("demo.moderator", "Demo", "Moderator", StaticRoleNames.Host.Moderator);
        var member = GetOrCreateUser("demo.member", "Demo", "Member", StaticRoleNames.Host.User);
        var banned = GetOrCreateUser("demo.banned", "Demo", "Restricted", StaticRoleNames.Host.User);
        _context.SaveChanges();

        var welcome = GetOrCreatePost(
            WelcomeSlug,
            "Welcome to the blog",
            "A short tour of what this blog is for and how the review workflow keeps quality high.",
            "# Welcome\n\n" +
            "This is the demo blog that ships with the application. Posts go through a\n" +
            "review workflow: an author writes a draft, submits it, and a moderator or\n" +
            "administrator approves it before it appears here.\n\n" +
            "## What you can do\n\n" +
            "- Read every published post without an account.\n" +
            "- Register to comment, reply, and upvote.\n" +
            "- Authors track their drafts in the workspace.\n\n" +
            "Content is written in Markdown and rendered through a sanitizer, so\n" +
            "headings, lists, links, and code are safe to use.",
            PostStatus.Approved,
            author.Id,
            Clock.Now.AddDays(-5));

        var markdownPost = GetOrCreatePost(
            MarkdownSlug,
            "Writing in Markdown",
            "How the editor, the live preview, and the renderer treat the Markdown you write.",
            "## Markdown in this blog\n\n" +
            "Every post, comment, and reply is Markdown. The editor has a preview toggle\n" +
            "so you can check the result before saving.\n\n" +
            "```csharp\n" +
            "var post = await _postAppService.CreateAsync(input);\n" +
            "```\n\n" +
            "Lists, links, and emphasis all work:\n\n" +
            "1. Write the draft.\n" +
            "2. Check the preview.\n" +
            "3. Submit it for review.",
            PostStatus.Approved,
            author.Id,
            Clock.Now.AddDays(-2));

        GetOrCreatePost(
            "draft-community-guidelines",
            "Draft: community guidelines",
            "An early draft of the guidelines we want to publish for commenters.",
            "## Draft\n\n" +
            "This post is still a draft. Its author can see it in the workspace and\n" +
            "submit it for review when it is ready.",
            PostStatus.Draft,
            author.Id,
            null);

        GetOrCreatePost(
            "proposed-comment-moderation-policy",
            "Proposed: comment moderation policy",
            "Waiting for a moderator to approve or reject this proposed policy.",
            "## Proposal\n\n" +
            "This post is pending review. A moderator can approve it to publish, or\n" +
            "reject it with a reason that the author will see.",
            PostStatus.PendingReview,
            author.Id,
            null);

        GetOrCreatePost(
            "rejected-off-topic-notes",
            "Rejected: off-topic notes",
            "A post that was rejected during review, kept so the author can revise it.",
            "## Rejected\n\n" +
            "The review turned this down. The author can edit it and submit it again.",
            PostStatus.Rejected,
            author.Id,
            null);

        GetOrCreatePost(
            "archived-launch-announcement",
            "Archived: launch announcement",
            "An older announcement that has been archived and no longer appears publicly.",
            "## Archived\n\n" +
            "Archived posts keep their history but are not listed on the public blog.",
            PostStatus.Archived,
            author.Id,
            Clock.Now.AddDays(-20));

        _context.SaveChanges();

        var authorReply = SeedDiscussion(welcome, author, member);
        SeedUpvotes(welcome, markdownPost, authorReply, author, moderator, member);
        SeedBan(banned, moderator);

        _context.SaveChanges();
    }

    private Comment SeedDiscussion(Post post, User author, User member)
    {
        // the author's reply is the post's only nested comment, so it doubles
        // as the marker for "this thread is already seeded"
        var existing = _context.Comments.IgnoreQueryFilters()
            .FirstOrDefault(c => c.PostId == post.Id && c.ParentCommentId != null);
        if (existing != null)
        {
            return existing;
        }

        var first = new Comment
        {
            Id = _guidGenerator.Create(),
            PostId = post.Id,
            UserId = member.Id,
            ContentMarkdown = "Great first post. The workflow explanation is clear."
        };
        _context.Comments.Add(first);
        _context.SaveChanges();

        var reply = new Comment
        {
            Id = _guidGenerator.Create(),
            PostId = post.Id,
            UserId = author.Id,
            ParentCommentId = first.Id,
            ContentMarkdown = "Thanks. More posts are on the way."
        };
        _context.Comments.Add(reply);

        _context.Comments.Add(new Comment
        {
            Id = _guidGenerator.Create(),
            PostId = post.Id,
            UserId = member.Id,
            ContentMarkdown = "The Markdown rendering looks right in both light and dark mode."
        });

        return reply;
    }

    private void SeedUpvotes(
        Post welcome,
        Post markdownPost,
        Comment authorReply,
        User author,
        User moderator,
        User member)
    {
        // Two votes on the older post, one on the newer one - so the "top" sort
        // and the "newest" sort produce visibly different orders in the demo.
        AddUpvoteIfMissing(member.Id, UpvoteTargetType.Post, welcome.Id);
        AddUpvoteIfMissing(moderator.Id, UpvoteTargetType.Post, welcome.Id);
        AddUpvoteIfMissing(member.Id, UpvoteTargetType.Post, markdownPost.Id);
        AddUpvoteIfMissing(author.Id, UpvoteTargetType.Comment, authorReply.Id);
    }

    private void SeedBan(User banned, User moderator)
    {
        var permissionName = PermissionNames.Blog.Comments_Create;

        var hasActiveBan = _context.UserBans.IgnoreQueryFilters()
            .Any(b => b.UserId == banned.Id && b.BanType == (int)BanType.Comment && b.LiftedAt == null);
        if (!hasActiveBan)
        {
            _context.UserBans.Add(new UserBan
            {
                Id = _guidGenerator.Create(),
                UserId = banned.Id,
                BanType = (int)BanType.Comment,
                Reason = "Off-topic comments in the demo thread.",
                BannedByUserId = moderator.Id
            });
        }

        // Enforcement lives in ABP's user-level permission settings - this is
        // exactly the row UserManager.ProhibitPermissionAsync writes (the
        // seeder cannot go through the unit-of-work-based UserManager API).
        var hasProhibition = _context.Permissions.IgnoreQueryFilters()
            .OfType<UserPermissionSetting>()
            .Any(p => p.UserId == banned.Id && p.Name == permissionName && !p.IsGranted);
        if (!hasProhibition)
        {
            _context.Permissions.Add(new UserPermissionSetting
            {
                TenantId = null,
                UserId = banned.Id,
                Name = permissionName,
                IsGranted = false
            });
        }
    }

    private void AddUpvoteIfMissing(long userId, UpvoteTargetType targetType, Guid targetId)
    {
        var exists = _context.Upvotes.IgnoreQueryFilters().Any(u =>
            u.UserId == userId && u.TargetType == (int)targetType && u.TargetId == targetId);
        if (!exists)
        {
            _context.Upvotes.Add(new Upvote
            {
                Id = _guidGenerator.Create(),
                UserId = userId,
                TargetType = (int)targetType,
                TargetId = targetId
            });
        }
    }

    private User GetOrCreateUser(string userName, string name, string surname, string roleName)
    {
        var existing = _context.Users.IgnoreQueryFilters()
            .FirstOrDefault(u => u.TenantId == null && u.UserName == userName);
        if (existing != null)
        {
            return existing;
        }

        var user = new User
        {
            TenantId = null,
            UserName = userName,
            Name = name,
            Surname = surname,
            EmailAddress = userName + "@blogapplication.com",
            IsEmailConfirmed = true,
            IsActive = true
        };

        user.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions()))
            .HashPassword(user, DemoPassword);
        user.SetNormalizedNames();

        _context.Users.Add(user);
        _context.SaveChanges();

        var role = _context.Roles.IgnoreQueryFilters()
            .First(r => r.TenantId == null && r.Name == roleName);
        _context.UserRoles.Add(new UserRole(null, user.Id, role.Id));

        return user;
    }

    private Post GetOrCreatePost(
        string slug,
        string title,
        string excerpt,
        string contentMarkdown,
        PostStatus status,
        long authorId,
        DateTime? publishedAt)
    {
        var existing = _context.Posts.IgnoreQueryFilters().FirstOrDefault(p => p.Slug == slug);
        if (existing != null)
        {
            return existing;
        }

        var post = new Post
        {
            Id = _guidGenerator.Create(),
            AuthorId = authorId,
            Title = title,
            Slug = slug,
            Excerpt = excerpt,
            ContentMarkdown = contentMarkdown,
            Status = status,
            PublishedAt = publishedAt
        };

        _context.Posts.Add(post);
        return post;
    }
}
