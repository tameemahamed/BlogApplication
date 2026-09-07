using Abp.Application.Services.Dto;

namespace BlogApplication.Posts.Dto;

public class GetPublicPostsInput : PagedResultRequestDto
{
    /// <summary>
    /// false (default) sorts by publish date (newest); true sorts by upvote
    /// count (top) - prd.md E5-S5.
    /// </summary>
    public bool SortByUpvotes { get; set; }
}
