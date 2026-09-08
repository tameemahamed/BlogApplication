using Abp.Application.Services.Dto;

namespace BlogApplication.Bans.Dto;

public class GetUserBansInput : PagedResultRequestDto
{
    /// <summary>
    /// Filter to one user's ban history (user management dialog).
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// true filters to currently-active bans (LiftedAt IS NULL).
    /// </summary>
    public bool OnlyActive { get; set; }
}
