using System;
using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Upvotes.Dto;

public class ToggleUpvoteInput
{
    /// <summary>
    /// int-backed UpvoteTargetType (plain number in the generated client).
    /// </summary>
    [Required]
    public int TargetType { get; set; }

    [Required]
    public Guid TargetId { get; set; }
}
