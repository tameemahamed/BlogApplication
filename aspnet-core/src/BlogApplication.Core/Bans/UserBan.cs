using Abp.Domain.Entities.Auditing;
using System;

namespace BlogApplication.Bans;

/// <summary>
/// The moderation ledger for one imposed ban (prd.md E6-S5, schema 3.4).
/// Display and audit only - it participates in NO enforcement; bans are
/// enforced by ABP user-level permission prohibitions that UserBanAppService
/// writes in the same unit of work. Rows are never deleted: unbanning stamps
/// LiftedAt/LiftedByUserId, preserving the full moderation history.
/// Creation-audited only (CreationTime/CreatorUserId record who banned and
/// when; no soft-delete columns - the ledger is never removed).
/// </summary>
public class UserBan : CreationAuditedEntity<Guid>
{
    public long UserId { get; set; }

    /// <summary>
    /// int-backed BanType (prd.md D10); maps 1:1 to the prohibited permission.
    /// </summary>
    public int BanType { get; set; }

    /// <summary>
    /// Human-readable reason shown to the banned user - the one thing ABP's
    /// permission system does not store.
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Moderator/admin who imposed the ban (equals CreatorUserId; kept
    /// explicit so moderation queries render without joins - prd.md D11).
    /// </summary>
    public long BannedByUserId { get; set; }

    /// <summary>
    /// NULL = the ban is currently active (per the ledger).
    /// </summary>
    public DateTime? LiftedAt { get; set; }

    public long? LiftedByUserId { get; set; }
}
