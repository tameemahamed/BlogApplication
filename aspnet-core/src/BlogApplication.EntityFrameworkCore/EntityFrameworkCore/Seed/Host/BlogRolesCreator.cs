using Abp.Authorization;
using Abp.Authorization.Roles;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Roles;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BlogApplication.EntityFrameworkCore.Seed.Host;

/// <summary>
/// Creates the application roles (Moderator, Author, User) and seeds their
/// permission grants per prd.md §2.2. The Admin role receives every permission
/// from <see cref="HostRoleAndUserCreator"/> instead.
/// Every role includes the community action permissions (prd.md A7); the User
/// role is the default role assigned on registration (prd.md A8).
/// </summary>
public class BlogRolesCreator
{
    private readonly BlogApplicationDbContext _context;

    public BlogRolesCreator(BlogApplicationDbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateRole(
            StaticRoleNames.Host.User,
            StaticRoleNames.Host.User,
            isDefault: true,
            PermissionNames.Blog.Comments_Create,
            PermissionNames.Blog.Comments_Edit,
            PermissionNames.Blog.Comments_Delete,
            PermissionNames.Blog.Replies_Create,
            PermissionNames.Blog.Upvotes_Toggle);

        CreateRole(
            StaticRoleNames.Host.Author,
            StaticRoleNames.Host.Author,
            isDefault: false,
            PermissionNames.Blog.Comments_Create,
            PermissionNames.Blog.Comments_Edit,
            PermissionNames.Blog.Comments_Delete,
            PermissionNames.Blog.Replies_Create,
            PermissionNames.Blog.Upvotes_Toggle,
            PermissionNames.Blog.Posts_Create,
            PermissionNames.Blog.Posts_Edit,
            PermissionNames.Blog.Posts_Delete,
            PermissionNames.Blog.Posts_Archive);

        CreateRole(
            StaticRoleNames.Host.Moderator,
            StaticRoleNames.Host.Moderator,
            isDefault: false,
            PermissionNames.Blog.Comments_Create,
            PermissionNames.Blog.Comments_Edit,
            PermissionNames.Blog.Comments_Delete,
            PermissionNames.Blog.Replies_Create,
            PermissionNames.Blog.Upvotes_Toggle,
            PermissionNames.Blog.Posts_Create,
            PermissionNames.Blog.Posts_Edit,
            PermissionNames.Blog.Posts_Delete,
            PermissionNames.Blog.Posts_Archive,
            PermissionNames.Blog.Posts_Approve,
            PermissionNames.Blog.Bans_Manage);
    }

    private void CreateRole(string name, string displayName, bool isDefault, params string[] permissionNames)
    {
        var role = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == null && r.Name == name);
        if (role == null)
        {
            role = _context.Roles.Add(new Role(null, name, displayName) { IsStatic = true, IsDefault = isDefault }).Entity;
            _context.SaveChanges();
        }

        var grantedPermissions = _context.Permissions.IgnoreQueryFilters()
            .OfType<RolePermissionSetting>()
            .Where(p => p.TenantId == null && p.RoleId == role.Id)
            .Select(p => p.Name)
            .ToList();

        var missingPermissions = permissionNames
            .Where(p => !grantedPermissions.Contains(p))
            .ToList();

        if (missingPermissions.Any())
        {
            _context.Permissions.AddRange(
                missingPermissions.Select(permissionName => new RolePermissionSetting
                {
                    TenantId = null,
                    Name = permissionName,
                    IsGranted = true,
                    RoleId = role.Id
                }));
            _context.SaveChanges();
        }
    }
}
