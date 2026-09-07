using Abp.Application.Services;
using BlogApplication.Upvotes.Dto;
using System.Threading.Tasks;

namespace BlogApplication.Upvotes;

public interface IUpvoteAppService : IApplicationService
{
    Task<ToggleUpvoteOutput> ToggleAsync(ToggleUpvoteInput input);
}
