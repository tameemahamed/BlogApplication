using Abp.Domain.Entities.Auditing;
using System;

namespace BlogApplication.Posts;

/// <summary>
/// A blog article authored by a user. Publicly visible only when
/// <see cref="Status"/> is <see cref="PostStatus.Approved"/> (prd.md §3.1).
/// UUID key generated via ABP's IGuidGenerator (prd.md A13).
/// </summary>
public class Post : FullAuditedEntity<Guid>
{
    public long AuthorId { get; set; }

    public string Title { get; set; }

    /// <summary>
    /// URL-safe identifier generated once from the title on creation;
    /// later title edits do not change it (stable URLs, prd.md E2-S3).
    /// </summary>
    public string Slug { get; set; }

    public string Excerpt { get; set; }

    /// <summary>
    /// Raw Markdown source only; HTML is never persisted (prd.md A-series / E3-S5).
    /// </summary>
    public string ContentMarkdown { get; set; }

    public PostStatus Status { get; set; }

    /// <summary>
    /// Populated on rejection; visible to the author only.
    /// </summary>
    public string RejectionReason { get; set; }

    /// <summary>
    /// Set when approved; cleared when the post goes back to review.
    /// </summary>
    public DateTime? PublishedAt { get; set; }
}
