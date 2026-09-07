using Abp;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using BlogApplication.Authorization;
using BlogApplication.Comments;
using BlogApplication.Posts;
using BlogApplication.Upvotes.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BlogApplication.Upvotes;

public class UpvoteAppService : BlogApplicationAppServiceBase, IUpvoteAppService
{
    private readonly IRepository<Upvote, Guid> _upvoteRepository;
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<Comment, Guid> _commentRepository;
    private readonly IGuidGenerator _guidGenerator;

    public UpvoteAppService(
        IRepository<Upvote, Guid> upvoteRepository,
        IRepository<Post, Guid> postRepository,
        IRepository<Comment, Guid> commentRepository,
        IGuidGenerator guidGenerator)
    {
        _upvoteRepository = upvoteRepository;
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _guidGenerator = guidGenerator;
    }

    /// <summary>
    /// Toggle on/off. Off physically deletes the row (prd.md D4) - the plain
    /// unique index then frees the slot for a future re-upvote. The toggle
    /// permission covers add AND remove (prd.md Q5). Ban enforcement is the
    /// [AbpAuthorize] attribute alone (prd.md E5-S4).
    /// </summary>
    [AbpAuthorize(PermissionNames.Blog.Upvotes_Toggle)]
    public async Task<ToggleUpvoteOutput> ToggleAsync(ToggleUpvoteInput input)
    {
        if (!Enum.IsDefined(typeof(UpvoteTargetType), input.TargetType))
        {
            throw new ArgumentOutOfRangeException(nameof(input.TargetType));
        }

        var targetType = (UpvoteTargetType)input.TargetType;
        await EnsureTargetExistsAsync(targetType, input.TargetId);

        var userId = AbpSession.GetUserId();
        var existing = await _upvoteRepository.FirstOrDefaultAsync(v =>
            v.UserId == userId &&
            v.TargetType == (int)targetType &&
            v.TargetId == input.TargetId);

        if (existing != null)
        {
            await _upvoteRepository.DeleteAsync(existing);
            await CurrentUnitOfWork.SaveChangesAsync();
            return await BuildOutputAsync(targetType, input.TargetId, false);
        }

        await _upvoteRepository.InsertAsync(new Upvote
        {
            Id = _guidGenerator.Create(),
            UserId = userId,
            TargetType = (int)targetType,
            TargetId = input.TargetId
        });
        await CurrentUnitOfWork.SaveChangesAsync();

        return await BuildOutputAsync(targetType, input.TargetId, true);
    }

    private async Task<ToggleUpvoteOutput> BuildOutputAsync(UpvoteTargetType targetType, Guid targetId, bool upvoted)
    {
        var count = await _upvoteRepository
            .GetAll()
            .CountAsync(v => v.TargetType == (int)targetType && v.TargetId == targetId);

        return new ToggleUpvoteOutput
        {
            TargetType = (int)targetType,
            TargetId = targetId,
            Upvoted = upvoted,
            UpvoteCount = count
        };
    }

    /// <summary>
    /// Upvotes only target visible content: approved posts, and comments on
    /// approved posts (defense in depth - prd.md E4-S8 class of rules).
    /// </summary>
    private async Task EnsureTargetExistsAsync(UpvoteTargetType targetType, Guid targetId)
    {
        switch (targetType)
        {
            case UpvoteTargetType.Post:
                var postExists = await _postRepository
                    .GetAll()
                    .AnyAsync(p => p.Id == targetId && p.Status == PostStatus.Approved);
                if (!postExists)
                {
                    throw new UserFriendlyException(L("PostNotFound"));
                }

                break;
            case UpvoteTargetType.Comment:
                var comment = await _commentRepository
                    .GetAll()
                    .FirstOrDefaultAsync(c => c.Id == targetId);
                if (comment == null)
                {
                    throw new EntityNotFoundException(typeof(Comment), targetId);
                }

                var postApproved = await _postRepository
                    .GetAll()
                    .AnyAsync(p => p.Id == comment.PostId && p.Status == PostStatus.Approved);
                if (!postApproved)
                {
                    throw new UserFriendlyException(L("PostNotFound"));
                }

                break;
        }
    }
}
