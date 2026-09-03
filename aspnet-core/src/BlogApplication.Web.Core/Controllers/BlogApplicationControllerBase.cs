using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace BlogApplication.Controllers
{
    public abstract class BlogApplicationControllerBase : AbpController
    {
        protected BlogApplicationControllerBase()
        {
            LocalizationSourceName = BlogApplicationConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
