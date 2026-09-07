using System.Collections.Generic;

namespace BlogApplication.Comments.Dto;

/// <summary>
/// A top-level comment with its reply subtree flattened underneath it,
/// ordered chronologically (prd.md A5 - one nesting level in the UI,
/// deeper replies carry an @author reference).
/// </summary>
public class TopLevelCommentDto : CommentDto
{
    public IReadOnlyList<CommentDto> Replies { get; set; }
}
