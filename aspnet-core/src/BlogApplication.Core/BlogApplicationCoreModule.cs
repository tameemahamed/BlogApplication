using Abp.Localization;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Security;
using Abp.Timing;
using Abp.Zero;
using Abp.Zero.Configuration;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.Configuration;
using BlogApplication.Localization;
using BlogApplication.MultiTenancy;
using BlogApplication.Timing;

namespace BlogApplication;

[DependsOn(typeof(AbpZeroCoreModule))]
public class BlogApplicationCoreModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Auditing.IsEnabledForAnonymousUsers = true;

        // Declare entity types
        Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
        Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
        Configuration.Modules.Zero().EntityTypes.User = typeof(User);

        BlogApplicationLocalizationConfigurer.Configure(Configuration.Localization);

        // Enable this line to create a multi-tenant application.
        Configuration.MultiTenancy.IsEnabled = BlogApplicationConsts.MultiTenancyEnabled;

        // Configure roles
        AppRoleConfig.Configure(Configuration.Modules.Zero().RoleManagement);

        Configuration.Settings.Providers.Add<AppSettingProvider>();

        Configuration.Localization.Languages.Add(new LanguageInfo("fa", "فارسی", "famfamfam-flags ir"));

        Configuration.Settings.SettingEncryptionConfiguration.DefaultPassPhrase = BlogApplicationConsts.DefaultPassPhrase;
        SimpleStringCipher.DefaultPassPhrase = BlogApplicationConsts.DefaultPassPhrase;
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(BlogApplicationCoreModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        Clock.Provider = ClockProviders.Utc;
        IocManager.Resolve<AppTimes>().StartupTime = Clock.Now;
    }
}
