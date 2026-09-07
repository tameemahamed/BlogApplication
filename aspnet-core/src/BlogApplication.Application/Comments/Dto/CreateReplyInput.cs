using System;
using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Comments.Dto;

/// <summary>
/// The PostId is derived from the parent comment, so cross-post replies are
/// structurally impossible (prd.md E4-S8).
/// </summary>
public class CreateReplyInput
{
    [Required]
    public Guid ParentCommentId { get; set; }

    [Required]
    [MaxLength(CommentConsts.MaxContentLength)]
    public string ContentMarkdown { get; set; }
}
