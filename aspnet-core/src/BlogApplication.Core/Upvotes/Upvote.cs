using Abp.Domain.Entities;
using System;

namespace BlogApplication.Upvotes;

/// <summary>
/// One user's upvote on a post, comment, or reply - toggleable.
/// A plain, non-audited entity: the row's existence is the entire state
/// (prd.md D4) - toggling off physically deletes the row, and the unique
/// index (UserId, TargetType, TargetId) is the anti-double-vote guarantee.
/// UUID key generated via ABP's IGuidGenerator (prd.md A13).
/// </summary>
public class Upvote : Entity<Guid>
{
    public long UserId { get; set; }

    /// <summary>
    /// int-backed UpvoteTargetType (prd.md D10).
    /// </summary>
    public int TargetType { get; set; }

    /// <summary>
    /// Id of the target post/comment - no FK (polymorphic); existence is
    /// validated in UpvoteAppService (prd.md D2).
    /// </summary>
    public Guid TargetId { get; set; }
}
