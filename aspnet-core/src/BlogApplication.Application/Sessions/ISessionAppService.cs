using Abp.Application.Services;
using BlogApplication.Sessions.Dto;
using System.Threading.Tasks;

namespace BlogApplication.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}
