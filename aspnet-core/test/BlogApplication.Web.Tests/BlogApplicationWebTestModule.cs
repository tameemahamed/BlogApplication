using Abp.AspNetCore;
using Abp.AspNetCore.TestBase;
using Abp.Modules;
using Abp.Reflection.Extensions;
using BlogApplication.EntityFrameworkCore;
using BlogApplication.Web.Startup;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace BlogApplication.Web.Tests;

[DependsOn(
    typeof(BlogApplicationWebMvcModule),
    typeof(AbpAspNetCoreTestBaseModule)
)]
public class BlogApplicationWebTestModule : AbpModule
{
    public BlogApplicationWebTestModule(BlogApplicationEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbContextRegistration = true;
    }

    public override void PreInitialize()
    {
        Configuration.UnitOfWork.IsTransactional = false; //EF Core InMemory DB does not support transactions.
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(BlogApplicationWebTestModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<ApplicationPartManager>()
            .AddApplicationPartsIfNotAddedBefore(typeof(BlogApplicationWebMvcModule).Assembly);
    }
}