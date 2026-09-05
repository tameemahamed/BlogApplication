using Abp.MultiTenancy;
using Abp.Zero.Configuration;

namespace BlogApplication.Authorization.Roles;

public static class AppRoleConfig
{
    public static void Configure(IRoleManagementConfig roleManagementConfig)
    {
        // Static host roles (single-tenant application)

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Host.Admin,
                MultiTenancySides.Host
            )
        );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Host.Moderator,
                MultiTenancySides.Host
            )
        );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Host.Author,
                MultiTenancySides.Host
            )
        );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Host.User,
                MultiTenancySides.Host
            )
        );
    }
}
