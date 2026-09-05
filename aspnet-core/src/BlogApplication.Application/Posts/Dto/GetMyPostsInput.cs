using Abp.Application.Services.Dto;

namespace BlogApplication.Posts.Dto;

public class GetMyPostsInput : PagedResultRequestDto
{
    /// <summary>
    /// Optional PostStatus filter (int to keep the generated client a plain number).
    /// </summary>
    public int? Status { get; set; }
}
