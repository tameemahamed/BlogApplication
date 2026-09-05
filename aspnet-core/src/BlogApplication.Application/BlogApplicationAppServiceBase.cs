using Abp.Application.Services;
using Abp.IdentityFramework;
using Abp.Runtime.Session;
using BlogApplication.Authorization.Users;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace BlogApplication;

/// <summary>
/// Derive your application services from this class.
/// </summary>
public abstract class BlogApplicationAppServiceBase : ApplicationService
{
    public UserManager UserManager { get; set; }

    protected BlogApplicationAppServiceBase()
    {
        LocalizationSourceName = BlogApplicationConsts.LocalizationSourceName;
    }

    protected virtual async Task<User> GetCurrentUserAsync()
    {
        var user = await UserManager.FindByIdAsync(AbpSession.GetUserId().ToString());
        if (user == null)
        {
            throw new Exception("There is no current user!");
        }

        return user;
    }

    protected virtual void CheckErrors(IdentityResult identityResult)
    {
        identityResult.CheckErrors(LocalizationManager);
    }
}
