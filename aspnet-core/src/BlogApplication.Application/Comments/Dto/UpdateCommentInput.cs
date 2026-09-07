using System;
using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Comments.Dto;

public class UpdateCommentInput
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(CommentConsts.MaxContentLength)]
    public string ContentMarkdown { get; set; }
}
