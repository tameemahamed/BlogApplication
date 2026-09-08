using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Bans.Dto;

public class UnbanInput
{
    [Required]
    public long UserId { get; set; }

    /// <summary>
    /// int-backed BanType (plain number in the generated client).
    /// </summary>
    [Required]
    public int BanType { get; set; }
}
