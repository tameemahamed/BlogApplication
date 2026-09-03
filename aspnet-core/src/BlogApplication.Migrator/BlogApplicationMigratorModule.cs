using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using BlogApplication.Configuration;
using BlogApplication.EntityFrameworkCore;
using BlogApplication.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;

namespace BlogApplication.Migrator;

[DependsOn(typeof(BlogApplicationEntityFrameworkModule))]
public class BlogApplicationMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public BlogApplicationMigratorModule(BlogApplicationEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(BlogApplicationMigratorModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            BlogApplicationConsts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(BlogApplicationMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
