using Abp.Application.Services;
using Abp.Application.Services.Dto;
using BlogApplication.Posts.Dto;
using System;
using System.Threading.Tasks;

namespace BlogApplication.Posts;

public interface IPostAppService : IApplicationService
{
    Task<PostDto> CreateAsync(CreatePostInput input);

    Task<PostDto> GetForEditAsync(Guid id);

    Task<PostDto> GetAsync(Guid id);

    Task UpdateAsync(UpdatePostInput input);

    Task SubmitAsync(Guid id);

    Task ApproveAsync(Guid id);

    Task RejectAsync(RejectPostInput input);

    Task ArchiveAsync(Guid id);

    Task DeleteAsync(Guid id);

    Task<PagedResultDto<PostDto>> GetMyPostsAsync(GetMyPostsInput input);

    Task<PagedResultDto<PostDto>> GetPendingReviewPostsAsync(PagedResultRequestDto input);

    Task<PagedResultDto<PublicPostListDto>> GetPublicPostsAsync(GetPublicPostsInput input);

    Task<PublicPostDetailDto> GetPublicPostBySlugAsync(string slug);
}
