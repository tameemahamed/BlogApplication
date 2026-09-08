using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Bans.Dto;

public class BanUserInput
{
    [Required]
    public long UserId { get; set; }

    /// <summary>
    /// One or more int-backed BanType values (plain numbers in the generated
    /// client) - a single action can ban several abilities at once. Each
    /// requested type gets its own prohibition and its own ledger row, so
    /// history and unban stay granular per ability.
    /// </summary>
    [Required]
    public List<int> BanTypes { get; set; }

    /// <summary>
    /// Shown to the banned user (the permission system stores no reason).
    /// </summary>
    [Required]
    [StringLength(UserBanConsts.MaxReasonLength)]
    public string Reason { get; set; }
}
