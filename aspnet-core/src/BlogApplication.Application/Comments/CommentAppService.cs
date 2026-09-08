using Abp;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Users;
using BlogApplication.Comments.Dto;
using BlogApplication.Posts;
using BlogApplication.Upvotes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogApplication.Comments;

public class CommentAppService : BlogApplicationAppServiceBase, ICommentAppService
{
    private readonly IRepository<Comment, Guid> _commentRepository;
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly IRepository<Upvote, Guid> _upvoteRepository;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IGuidGenerator _guidGenerator;

    public CommentAppService(
        IRepository<Comment, Guid> commentRepository,
        IRepository<Post, Guid> postRepository,
        IRepository<User, long> userRepository,
        IRepository<Upvote, Guid> upvoteRepository,
        IPermissionChecker permissionChecker,
        IGuidGenerator guidGenerator)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _userRepository = userRepository;
        _upvoteRepository = upvoteRepository;
        _permissionChecker = permissionChecker;
        _guidGenerator = guidGenerator;
    }

    [AbpAuthorize(PermissionNames.Blog.Comments_Create)]
    public async Task<CommentDto> CreateCommentAsync(CreateCommentInput input)
    {
        await GetApprovedPostOrThrowAsync(input.PostId);

        var comment = new Comment
        {
            Id = _guidGenerator.Create(),
            PostId = input.PostId,
            UserId = AbpSession.GetUserId(),
            ContentMarkdown = input.ContentMarkdown
        };

        await _commentRepository.InsertAsync(comment);
        await CurrentUnitOfWork.SaveChangesAsync();

        return MapDto(comment, await GetCurrentUserAsync().ConfigureAwait(false));
    }

    [AbpAuthorize(PermissionNames.Blog.Replies_Create)]
    public async Task<CommentDto> CreateReplyAsync(CreateReplyInput input)
    {
        // Soft-deleted parents are already filtered out - a reply can only
        // attach to a visible comment
        var parent = await _commentRepository.GetAsync(input.ParentCommentId);
        await GetApprovedPostOrThrowAsync(parent.PostId);

        // PostId is derived from the parent, so cross-post replies are
        // structurally impossible (prd.md E4-S8)
        var reply = new Comment
        {
            Id = _guidGenerator.Create(),
            PostId = parent.PostId,
            UserId = AbpSession.GetUserId(),
            ParentCommentId = parent.Id,
            ContentMarkdown = input.ContentMarkdown
        };

        await _commentRepository.InsertAsync(reply);
        await CurrentUnitOfWork.SaveChangesAsync();

        return MapDto(reply, await GetCurrentUserAsync().ConfigureAwait(false));
    }

    [AbpAuthorize(PermissionNames.Blog.Comments_Edit)]
    public async Task UpdateCommentAsync(UpdateCommentInput input)
    {
        var comment = await _commentRepository.GetAsync(input.Id);

        if (comment.UserId != AbpSession.GetUserId())
        {
            throw new AbpAuthorizationException(L("CommentModifyNotAllowed"));
        }

        comment.ContentMarkdown = input.ContentMarkdown;
        comment.IsEdited = true;
    }

    [AbpAuthorize(PermissionNames.Blog.Comments_Delete)]
    public async Task DeleteCommentAsync(Guid id)
    {
        var comment = await _commentRepository.GetAsync(id);
        await CheckDeleteAuthorityAsync(comment);

        // Deleting a comment hides its entire reply subtree from public
        // view (prd.md E4-S6) - cascade the soft delete to descendants
        await SoftDeleteDescendantsAsync(comment);

        await _commentRepository.DeleteAsync(comment);
    }

    [AbpAllowAnonymous]
    public async Task<CommentThreadDto> GetCommentThreadAsync(GetCommentThreadInput input)
    {
        // Only approved posts expose their discussion - unapproved or
        // archived posts never leak comments through this endpoint
        var postApproved = await _postRepository
            .GetAll()
            .AnyAsync(p => p.Id == input.PostId && p.Status == PostStatus.Approved);

        if (!postApproved)
        {
            return new CommentThreadDto
            {
                TotalCount = 0,
                TopLevelCount = 0,
                Comments = new List<TopLevelCommentDto>()
            };
        }

        // A post's discussion is bounded - load it once and build the tree
        // in memory (top-level paging happens on the projected slice)
        var comments = await _commentRepository
            .GetAll()
            .Where(c => c.PostId == input.PostId)
            .OrderBy(c => c.CreationTime)
            .ToListAsync();

        var userNames = await GetUserNamesAsync(comments.Select(c => c.UserId).Distinct());
        var (upvoteCounts, myUpvotes) = await GetUpvoteDataAsync(
            UpvoteTargetType.Comment,
            comments.Select(c => c.Id));

        var topLevel = comments
            .Where(c => c.ParentCommentId == null)
            .OrderByDescending(c => c.CreationTime)
            .ToList();

        var childrenMap = comments
            .Where(c => c.ParentCommentId != null)
            .GroupBy(c => c.ParentCommentId.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreationTime).ToList());

        // Public endpoint: callers that omit paging get a sane page size
        // rather than an empty result (Take(0))
        var pageSize = input.MaxResultCount > 0 ? input.MaxResultCount : 10;

        var page = topLevel
            .Skip(input.SkipCount)
            .Take(pageSize)
            .Select(top =>
            {
                var dto = MapDto(top, userNames, upvoteCounts, myUpvotes) as TopLevelCommentDto;
                var replies = new List<CommentDto>();
                CollectDescendants(top.Id, childrenMap, replies, userNames, upvoteCounts, myUpvotes);
                dto.Replies = replies;
                return dto;
            })
            .ToList();

        return new CommentThreadDto
        {
            TotalCount = comments.Count,
            TopLevelCount = topLevel.Count,
            Comments = page
        };
    }

    /// <summary>
    /// Moderation overview feed (prd.md E6-S6): the newest comments across
    /// all posts with their post title, for day-to-day content oversight.
    /// </summary>
    [AbpAuthorize(PermissionNames.Blog.Bans_Manage)]
    public async Task<PagedResultDto<ModerationCommentDto>> GetRecentCommentsForModerationAsync(
        PagedResultRequestDto input)
    {
        var query = _commentRepository.GetAll();

        var totalCount = await query.CountAsync();
        var comments = await query
            .OrderByDescending(c => c.CreationTime)
            .PageBy(input)
            .ToListAsync();

        var postIds = comments.Select(c => c.PostId).Distinct().ToList();
        var postTitles = await _postRepository.GetAll()
            .Where(p => postIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Title);

        var userNames = await GetUserNamesAsync(comments.Select(c => c.UserId).Distinct());
        var (upvoteCounts, myUpvotes) = await GetUpvoteDataAsync(
            UpvoteTargetType.Comment,
            comments.Select(c => c.Id));

        var items = comments
            .Select(c => new ModerationCommentDto
            {
                Id = c.Id,
                PostId = c.PostId,
                PostTitle = postTitles.GetValueOrDefault(c.PostId),
                UserId = c.UserId,
                UserName = userNames.GetValueOrDefault(c.UserId) ?? L("UnknownAuthor"),
                ParentCommentId = c.ParentCommentId,
                ContentMarkdown = c.ContentMarkdown,
                IsEdited = c.IsEdited,
                CreationTime = c.CreationTime,
                UpvoteCount = upvoteCounts.GetValueOrDefault(c.Id),
                UpvotedByCurrentUser = AbpSession.UserId.HasValue
                    ? myUpvotes.Contains(c.Id)
                    : (bool?)null
            })
            .ToList();

        return new PagedResultDto<ModerationCommentDto>(totalCount, items);
    }

    /// <summary>
    /// Deletion authority (prd.md A10): commenters delete their own
    /// comments; the post's author deletes any comment on their own post;
    /// moderators and admins (holders of the approval permission) delete any.
    /// </summary>
    private async Task CheckDeleteAuthorityAsync(Comment comment)
    {
        var currentUserId = AbpSession.GetUserId();

        if (comment.UserId == currentUserId)
        {
            return;
        }

        var post = await _postRepository.GetAsync(comment.PostId);
        if (post.AuthorId == currentUserId)
        {
            return;
        }

        if (!await _permissionChecker.IsGrantedAsync(PermissionNames.Blog.Posts_Approve))
        {
            throw new AbpAuthorizationException(L("CommentDeleteNotAllowed"));
        }
    }

    private async Task SoftDeleteDescendantsAsync(Comment comment)
    {
        var descendants = await _commentRepository
            .GetAll()
            .Where(c => c.PostId == comment.PostId && c.ParentCommentId != null)
            .Select(c => new { c.Id, c.ParentCommentId })
            .ToListAsync();

        var childrenMap = descendants
            .GroupBy(d => d.ParentCommentId.Value)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToList());

        var toDelete = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(comment.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenMap.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var childId in children)
            {
                toDelete.Add(childId);
                queue.Enqueue(childId);
            }
        }

        foreach (var descendantId in toDelete)
        {
            await _commentRepository.DeleteAsync(c => c.Id == descendantId);
        }
    }

    private void CollectDescendants(
        Guid parentId,
        Dictionary<Guid, List<Comment>> childrenMap,
        List<CommentDto> replies,
        Dictionary<long, string> userNames,
        Dictionary<Guid, int> upvoteCounts,
        HashSet<Guid> myUpvotes)
    {
        if (!childrenMap.TryGetValue(parentId, out var children))
        {
            return;
        }

        foreach (var child in children)
        {
            replies.Add(MapDto(child, userNames, upvoteCounts, myUpvotes));
            CollectDescendants(child.Id, childrenMap, replies, userNames, upvoteCounts, myUpvotes);
        }
    }

    private async Task<(Dictionary<Guid, int> Counts, HashSet<Guid> Mine)> GetUpvoteDataAsync(
        UpvoteTargetType targetType,
        IEnumerable<Guid> targetIds)
    {
        var ids = targetIds.ToList();
        if (!ids.Any())
        {
            return (new Dictionary<Guid, int>(), new HashSet<Guid>());
        }

        var counts = await _upvoteRepository
            .GetAll()
            .Where(u => u.TargetType == (int)targetType && ids.Contains(u.TargetId))
            .GroupBy(u => u.TargetId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var mine = new HashSet<Guid>();
        if (AbpSession.UserId.HasValue)
        {
            var myUpvotes = await _upvoteRepository
                .GetAll()
                .Where(u => u.UserId == AbpSession.UserId.Value &&
                            u.TargetType == (int)targetType &&
                            ids.Contains(u.TargetId))
                .Select(u => u.TargetId)
                .ToListAsync();
            mine = new HashSet<Guid>(myUpvotes);
        }

        return (counts, mine);
    }

    private async Task<Post> GetApprovedPostOrThrowAsync(Guid postId)
    {
        var post = await _postRepository
            .GetAll()
            .FirstOrDefaultAsync(p => p.Id == postId && p.Status == PostStatus.Approved);

        if (post == null)
        {
            throw new UserFriendlyException(L("PostNotApprovedForComments"));
        }

        return post;
    }

    private async Task<Dictionary<long, string>> GetUserNamesAsync(IEnumerable<long> userIds)
    {
        var ids = userIds.ToList();
        if (!ids.Any())
        {
            return new Dictionary<long, string>();
        }

        return await _userRepository
            .GetAll()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);
    }

    private TopLevelCommentDto MapDto(Comment comment, User currentUser)
    {
        return new TopLevelCommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            UserId = comment.UserId,
            UserName = currentUser.UserName,
            ParentCommentId = comment.ParentCommentId,
            ContentMarkdown = comment.ContentMarkdown,
            IsEdited = comment.IsEdited,
            CreationTime = comment.CreationTime,
            Replies = new List<CommentDto>(),
            // the creating user is authenticated by definition and a fresh
            // comment carries no upvotes
            UpvoteCount = 0,
            UpvotedByCurrentUser = false
        };
    }

    private TopLevelCommentDto MapDto(
        Comment comment,
        Dictionary<long, string> userNames,
        Dictionary<Guid, int> upvoteCounts,
        HashSet<Guid> myUpvotes)
    {
        return new TopLevelCommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            UserId = comment.UserId,
            UserName = userNames.GetValueOrDefault(comment.UserId) ?? L("UnknownAuthor"),
            ParentCommentId = comment.ParentCommentId,
            ContentMarkdown = comment.ContentMarkdown,
            IsEdited = comment.IsEdited,
            CreationTime = comment.CreationTime,
            Replies = new List<CommentDto>(),
            // the flag is null for anonymous callers (prd.md A1)
            UpvoteCount = upvoteCounts.GetValueOrDefault(comment.Id),
            UpvotedByCurrentUser = AbpSession.UserId.HasValue
                ? myUpvotes.Contains(comment.Id)
                : (bool?)null
        };
    }
}
