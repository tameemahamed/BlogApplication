using Abp.Authorization;
using Abp.Localization;

namespace BlogApplication.Authorization;

public class BlogApplicationAuthorizationProvider : AuthorizationProvider
{
    public override void SetPermissions(IPermissionDefinitionContext context)
    {
        // Single-tenant application: permissions are side-agnostic (MultiTenancySides.All,
        // the default). With multi-tenancy disabled ABP resolves the current side to
        // Tenant for every session (AbpUserManager.GetCurrentMultiTenancySide), so
        // Host-only permissions would be denied for every user - including Admin.

        context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
        context.CreatePermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));
        context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));

        // Blog permissions (see prd.md §2.2). Comment/reply/upvote actions are separate
        // permissions so bans - user-level prohibitions - stay granular per action type.

        context.CreatePermission(PermissionNames.Blog.Posts_Create, L("CreatePosts"));
        context.CreatePermission(PermissionNames.Blog.Posts_Edit, L("EditPosts"));
        context.CreatePermission(PermissionNames.Blog.Posts_Delete, L("DeletePosts"));
        context.CreatePermission(PermissionNames.Blog.Posts_Archive, L("ArchivePosts"));
        context.CreatePermission(PermissionNames.Blog.Posts_Approve, L("ApprovePosts"));

        context.CreatePermission(PermissionNames.Blog.Comments_Create, L("CreateComments"));
        context.CreatePermission(PermissionNames.Blog.Comments_Edit, L("EditComments"));
        context.CreatePermission(PermissionNames.Blog.Comments_Delete, L("DeleteComments"));

        context.CreatePermission(PermissionNames.Blog.Replies_Create, L("CreateReplies"));

        context.CreatePermission(PermissionNames.Blog.Upvotes_Toggle, L("Upvote"));

        context.CreatePermission(PermissionNames.Blog.Bans_Manage, L("ManageBans"));
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, BlogApplicationConsts.LocalizationSourceName);
    }
}
