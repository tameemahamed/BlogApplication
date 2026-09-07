using Abp.Domain.Entities.Auditing;
using System;

namespace BlogApplication.Comments;

/// <summary>
/// A comment on a post, or a reply to another comment (self-referencing
/// ParentCommentId - prd.md §3.1). Published immediately (prd.md A2);
/// soft delete only (prd.md A11).
/// UUID key generated via ABP's IGuidGenerator (prd.md A13).
/// </summary>
public class Comment : FullAuditedEntity<Guid>
{
    public Guid PostId { get; set; }

    public long UserId { get; set; }

    /// <summary>
    /// Null for top-level comments; otherwise the comment or reply being
    /// replied to. Replies may target a comment or another reply (prd.md A5).
    /// </summary>
    public Guid? ParentCommentId { get; set; }

    /// <summary>
    /// Raw Markdown source only; HTML is never persisted (prd.md E3-S5).
    /// </summary>
    public string ContentMarkdown { get; set; }

    /// <summary>
    /// Set when the author edits their own comment (prd.md E4-S5).
    /// </summary>
    public bool IsEdited { get; set; }
}
