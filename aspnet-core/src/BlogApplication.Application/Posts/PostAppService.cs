using Abp;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Users;
using BlogApplication.Posts.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogApplication.Posts;

public class PostAppService : BlogApplicationAppServiceBase, IPostAppService
{
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly ISlugGenerator _slugGenerator;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IGuidGenerator _guidGenerator;

    public PostAppService(
        IRepository<Post, Guid> postRepository,
        IRepository<User, long> userRepository,
        ISlugGenerator slugGenerator,
        IPermissionChecker permissionChecker,
        IGuidGenerator guidGenerator)
    {
        _postRepository = postRepository;
        _userRepository = userRepository;
        _slugGenerator = slugGenerator;
        _permissionChecker = permissionChecker;
        _guidGenerator = guidGenerator;
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Create)]
    public async Task<PostDto> CreateAsync(CreatePostInput input)
    {
        var author = await GetCurrentUserAsync();

        var post = new Post
        {
            Id = _guidGenerator.Create(),
            AuthorId = AbpSession.GetUserId(),
            Title = input.Title,
            Slug = await _slugGenerator.GenerateAsync(input.Title),
            Excerpt = input.Excerpt,
            ContentMarkdown = input.ContentMarkdown,
            Status = PostStatus.Draft
        };

        await _postRepository.InsertAsync(post);
        await CurrentUnitOfWork.SaveChangesAsync();

        var dto = ObjectMapper.Map<PostDto>(post);
        dto.AuthorUserName = author.UserName;
        return dto;
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Edit)]
    public async Task<PostDto> GetForEditAsync(Guid id)
    {
        var post = await _postRepository.GetAsync(id);
        await CheckModifyPermissionAsync(post);

        return await MapToDtoAsync(post);
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Edit)]
    public async Task UpdateAsync(UpdatePostInput input)
    {
        var post = await _postRepository.GetAsync(input.Id);
        await CheckModifyPermissionAsync(post);

        post.Title = input.Title;
        post.Excerpt = input.Excerpt;
        post.ContentMarkdown = input.ContentMarkdown;

        // Editing a published post sends it back to review (prd.md A4).
        // Draft/pending/rejected posts keep their status; archived posts stay
        // archived until resubmitted.
        if (post.Status == PostStatus.Approved)
        {
            post.Status = PostStatus.PendingReview;
            post.PublishedAt = null;
        }

        await _postRepository.UpdateAsync(post);
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Create)]
    public async Task SubmitAsync(Guid id)
    {
        var post = await _postRepository.GetAsync(id);
        await CheckModifyPermissionAsync(post);

        if (post.Status != PostStatus.Draft &&
            post.Status != PostStatus.Rejected &&
            post.Status != PostStatus.Archived)
        {
            throw new UserFriendlyException(L("PostIsNotSubmittable"));
        }

        post.Status = PostStatus.PendingReview;
        post.RejectionReason = null;
        post.PublishedAt = null;
        await _postRepository.UpdateAsync(post);
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Approve)]
    public async Task ApproveAsync(Guid id)
    {
        var post = await _postRepository.GetAsync(id);

        if (post.Status != PostStatus.PendingReview)
        {
            throw new UserFriendlyException(L("PostIsNotPendingReview"));
        }

        post.Status = PostStatus.Approved;
        post.RejectionReason = null;
        post.PublishedAt = Clock.Provider.Now;
        await _postRepository.UpdateAsync(post);
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Approve)]
    public async Task RejectAsync(RejectPostInput input)
    {
        var post = await _postRepository.GetAsync(input.Id);

        if (post.Status != PostStatus.PendingReview)
        {
            throw new UserFriendlyException(L("PostIsNotPendingReview"));
        }

        post.Status = PostStatus.Rejected;
        post.RejectionReason = input.Reason;
        await _postRepository.UpdateAsync(post);
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Archive)]
    public async Task ArchiveAsync(Guid id)
    {
        var post = await _postRepository.GetAsync(id);
        await CheckModifyPermissionAsync(post);

        if (post.Status != PostStatus.Approved)
        {
            throw new UserFriendlyException(L("PostIsNotApproved"));
        }

        post.Status = PostStatus.Archived;
        await _postRepository.UpdateAsync(post);
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var post = await _postRepository.GetAsync(id);

        if (post.AuthorId == AbpSession.GetUserId())
        {
            // Authors archive live posts instead of deleting them (prd.md E2-S8)
            if (post.Status == PostStatus.Approved)
            {
                throw new UserFriendlyException(L("ApprovedPostCannotBeDeletedByAuthor"));
            }
        }
        else
        {
            await CheckModifyPermissionAsync(post);
        }

        await _postRepository.DeleteAsync(post);
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Create)]
    public async Task<PagedResultDto<PostDto>> GetMyPostsAsync(GetMyPostsInput input)
    {
        var author = await GetCurrentUserAsync();

        var query = _postRepository
            .GetAll()
            .Where(p => p.AuthorId == AbpSession.GetUserId())
            .WhereIf(input.Status.HasValue, p => (int)p.Status == input.Status.Value);

        var totalCount = await query.CountAsync();
        var posts = await query
            .OrderByDescending(p => p.CreationTime)
            .PageBy(input)
            .ToListAsync();

        var items = posts.Select(p => MapToDto(p, author.UserName)).ToList();
        return new PagedResultDto<PostDto>(totalCount, items);
    }

    [AbpAuthorize(PermissionNames.Blog.Posts_Approve)]
    public async Task<PagedResultDto<PostDto>> GetPendingReviewPostsAsync(PagedResultRequestDto input)
    {
        var query = _postRepository.GetAll().Where(p => p.Status == PostStatus.PendingReview);

        var totalCount = await query.CountAsync();
        var posts = await query
            .OrderBy(p => p.CreationTime) // oldest submissions first
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PostDto>(totalCount, await MapToDtosAsync(posts));
    }

    [AbpAllowAnonymous]
    public async Task<PagedResultDto<PublicPostListDto>> GetPublicPostsAsync(PagedResultRequestDto input)
    {
        var query = _postRepository.GetAll().Where(p => p.Status == PostStatus.Approved);

        var totalCount = await query.CountAsync();
        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .PageBy(input)
            .ToListAsync();

        var authorNames = await GetAuthorUserNamesAsync(posts);
        return new PagedResultDto<PublicPostListDto>(
            totalCount,
            posts.Select(p => MapToPublicListDto(p, authorNames)).ToList());
    }

    [AbpAllowAnonymous]
    public async Task<PublicPostDetailDto> GetPublicPostBySlugAsync(string slug)
    {
        var post = await _postRepository
            .GetAll()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PostStatus.Approved);

        if (post == null)
        {
            throw new EntityNotFoundException(typeof(Post), slug);
        }

        var authorNames = await GetAuthorUserNamesAsync(new[] { post });
        return MapToPublicDetailDto(post, authorNames);
    }

    /// <summary>
    /// Authors may only modify their own posts; moderators and admins
    /// (holders of the approval permission) may manage any post (prd.md §2.2).
    /// </summary>
    private async Task CheckModifyPermissionAsync(Post post)
    {
        if (post.AuthorId == AbpSession.GetUserId())
        {
            return;
        }

        if (!await _permissionChecker.IsGrantedAsync(PermissionNames.Blog.Posts_Approve))
        {
            throw new AbpAuthorizationException(L("PostModifyNotAllowed"));
        }
    }

    private async Task<Dictionary<long, string>> GetAuthorUserNamesAsync(IEnumerable<Post> posts)
    {
        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
        if (!authorIds.Any())
        {
            return new Dictionary<long, string>();
        }

        return await _userRepository
            .GetAll()
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);
    }

    private async Task<List<PostDto>> MapToDtosAsync(List<Post> posts)
    {
        var authorNames = await GetAuthorUserNamesAsync(posts);
        return posts
            .Select(p => MapToDto(p, GetAuthorName(authorNames, p.AuthorId)))
            .ToList();
    }

    private async Task<PostDto> MapToDtoAsync(Post post)
    {
        var authorNames = await GetAuthorUserNamesAsync(new[] { post });
        return MapToDto(post, GetAuthorName(authorNames, post.AuthorId));
    }

    private string GetAuthorName(Dictionary<long, string> authorNames, long authorId)
    {
        return authorNames.GetValueOrDefault(authorId) ?? L("UnknownAuthor");
    }

    private PostDto MapToDto(Post post, string authorUserName)
    {
        var dto = ObjectMapper.Map<PostDto>(post);
        dto.AuthorUserName = authorUserName;
        return dto;
    }

    private PublicPostListDto MapToPublicListDto(Post post, Dictionary<long, string> authorNames)
    {
        var dto = ObjectMapper.Map<PublicPostListDto>(post);
        dto.AuthorUserName = GetAuthorName(authorNames, post.AuthorId);
        return dto;
    }

    private PublicPostDetailDto MapToPublicDetailDto(Post post, Dictionary<long, string> authorNames)
    {
        var dto = ObjectMapper.Map<PublicPostDetailDto>(post);
        dto.AuthorUserName = GetAuthorName(authorNames, post.AuthorId);
        return dto;
    }
}
