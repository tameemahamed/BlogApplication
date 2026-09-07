using System;
using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Comments.Dto;

public class CreateCommentInput
{
    [Required]
    public Guid PostId { get; set; }

    [Required]
    [MaxLength(CommentConsts.MaxContentLength)]
    public string ContentMarkdown { get; set; }
}
