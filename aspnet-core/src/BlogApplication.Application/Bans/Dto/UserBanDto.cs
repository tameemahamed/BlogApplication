using System;

namespace BlogApplication.Bans.Dto;

/// <summary>
/// One ledger row for the management lists (user management dialog,
/// moderation overview). Includes the ban reason and actor names - the full
/// moderation audit trail (prd.md E6-S5).
/// </summary>
public class UserBanDto
{
    public Guid Id { get; set; }

    public long UserId { get; set; }

    public string UserName { get; set; }

    public int BanType { get; set; }

    public string Reason { get; set; }

    public DateTime CreationTime { get; set; }

    public string BannedByUserName { get; set; }

    public DateTime? LiftedAt { get; set; }

    public string LiftedByUserName { get; set; }
}
