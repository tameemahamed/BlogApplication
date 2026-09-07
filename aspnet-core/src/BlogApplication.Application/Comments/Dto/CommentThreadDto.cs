using System.Collections.Generic;

namespace BlogApplication.Comments.Dto;

/// <summary>
/// The visible discussion of a post: paged top-level comments (newest first)
/// with their reply subtrees, plus the total visible comment count including
/// replies (prd.md E4-S4).
/// </summary>
public class CommentThreadDto
{
    public int TotalCount { get; set; }

    public int TopLevelCount { get; set; }

    public IReadOnlyList<TopLevelCommentDto> Comments { get; set; }
}
