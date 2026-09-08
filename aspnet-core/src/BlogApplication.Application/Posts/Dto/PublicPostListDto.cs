using Abp.Application.Services.Dto;
using System;

namespace BlogApplication.Posts.Dto;

/// <summary>
/// Anonymous list view of an approved post (no content; the detail page loads it by slug).
/// </summary>
public class PublicPostListDto : EntityDto<Guid>
{
    public string Title { get; set; }

    public string Slug { get; set; }

    public string Excerpt { get; set; }

    public string AuthorUserName { get; set; }

    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Populated by PostAppService (prd.md E5-S3). The flag is null for
    /// anonymous callers (prd.md A1).
    /// </summary>
    public int UpvoteCount { get; set; }

    public bool? UpvotedByCurrentUser { get; set; }

    /// <summary>
    /// All visible comments of the post, replies included - matches the
    /// thread's total (prd.md E7-S1).
    /// </summary>
    public int CommentCount { get; set; }
}
