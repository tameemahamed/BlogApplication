using Abp.Application.Services.Dto;
using Abp.Application.Services;
using BlogApplication.Bans.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApplication.Bans;

public interface IUserBanAppService : IApplicationService
{
    Task BanAsync(BanUserInput input);

    Task UnbanAsync(UnbanInput input);

    /// <summary>
    /// The current user's active restrictions, read from the ledger (prd.md
    /// E6-S3) - drives the banned-user banner and the banned/not-permitted
    /// distinction on the client (prd.md E6-S4).
    /// </summary>
    Task<List<ActiveUserBanDto>> GetMyActiveBansAsync();

    Task<PagedResultDto<UserBanDto>> GetUserBansAsync(GetUserBansInput input);
}
