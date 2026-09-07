using Abp.Application.Services.Dto;
using System;

namespace BlogApplication.Posts.Dto;

/// <summary>
/// Anonymous detail view of an approved post, looked up by slug.
/// </summary>
public class PublicPostDetailDto : EntityDto<Guid>
{
    public long AuthorId { get; set; }

    public string Title { get; set; }

    public string Slug { get; set; }

    public string Excerpt { get; set; }

    public string ContentMarkdown { get; set; }

    public string AuthorUserName { get; set; }

    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Populated by PostAppService (prd.md E5-S3). The flag is null for
    /// anonymous callers (prd.md A1).
    /// </summary>
    public int UpvoteCount { get; set; }

    public bool? UpvotedByCurrentUser { get; set; }
}
