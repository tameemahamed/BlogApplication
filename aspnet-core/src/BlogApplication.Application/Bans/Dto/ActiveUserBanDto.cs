using System;

namespace BlogApplication.Bans.Dto;

/// <summary>
/// One active restriction of the current user - read from the ledger (the
/// permission system stores no reason). Drives the UI banner (prd.md E6-S3)
/// and the banned/not-permitted distinction (prd.md E6-S4).
/// </summary>
public class ActiveUserBanDto
{
    public int BanType { get; set; }

    public string Reason { get; set; }

    public DateTime CreationTime { get; set; }
}
