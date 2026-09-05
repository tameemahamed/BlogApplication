using System;
using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Posts.Dto;

public class RejectPostInput
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(PostConsts.MaxRejectionReasonLength)]
    public string Reason { get; set; }
}
