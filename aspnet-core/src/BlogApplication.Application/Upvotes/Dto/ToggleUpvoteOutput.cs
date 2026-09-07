using System;

namespace BlogApplication.Upvotes.Dto;

/// <summary>
/// Result of a toggle: the new state plus the fresh upvote count, so the UI
/// can update the target instantly without refetching (prd.md E5-S2).
/// </summary>
public class ToggleUpvoteOutput
{
    public int TargetType { get; set; }

    public Guid TargetId { get; set; }

    public bool Upvoted { get; set; }

    public int UpvoteCount { get; set; }
}
