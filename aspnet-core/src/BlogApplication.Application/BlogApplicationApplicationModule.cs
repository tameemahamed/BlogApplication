using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using BlogApplication.Authorization;

namespace BlogApplication;

[DependsOn(
    typeof(BlogApplicationCoreModule),
    typeof(AbpAutoMapperModule))]
public class BlogApplicationApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Authorization.Providers.Add<BlogApplicationAuthorizationProvider>();
    }

    public override void Initialize()
    {
        var thisAssembly = typeof(BlogApplicationApplicationModule).GetAssembly();

        IocManager.RegisterAssemblyByConvention(thisAssembly);

        Configuration.Modules.AbpAutoMapper().Configurators.Add(
            // Scan the assembly for classes which inherit from AutoMapper.Profile
            cfg => cfg.AddMaps(thisAssembly)
        );
    }
}
