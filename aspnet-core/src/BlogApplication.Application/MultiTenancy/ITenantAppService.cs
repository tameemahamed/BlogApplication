using Abp.Application.Services;
using BlogApplication.MultiTenancy.Dto;

namespace BlogApplication.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}

