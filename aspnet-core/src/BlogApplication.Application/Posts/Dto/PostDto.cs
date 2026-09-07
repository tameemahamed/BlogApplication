using Abp.Application.Services.Dto;
using System;

namespace BlogApplication.Posts.Dto;

/// <summary>
/// Author/editor view of a post. <see cref="Status"/> is exposed as int so the
/// generated TypeScript proxies stay plain numbers (labels are resolved via
/// localization on the client).
/// </summary>
public class PostDto : EntityDto<Guid>
{
    public long AuthorId { get; set; }

    public string AuthorUserName { get; set; }

    public string Title { get; set; }

    public string Slug { get; set; }

    public string Excerpt { get; set; }

    public string ContentMarkdown { get; set; }

    public int Status { get; set; }

    public string RejectionReason { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Populated by PostAppService (prd.md E5-S3). The flag is null for
    /// anonymous callers (prd.md A1).
    /// </summary>
    public int UpvoteCount { get; set; }

    public bool? UpvotedByCurrentUser { get; set; }
}
