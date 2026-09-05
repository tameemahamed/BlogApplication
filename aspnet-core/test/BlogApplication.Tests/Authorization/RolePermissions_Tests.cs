using Abp.Authorization;
using Abp.MultiTenancy;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace BlogApplication.Tests.Authorization;

/// <summary>
/// Verifies the seeded role-permission grants (prd.md §2.2) and the single-tenancy
/// permission definitions: with multi-tenancy disabled, the current side always
/// resolves to Tenant, so every permission must be side-agnostic (MultiTenancySides.All)
/// to be grantable at all - Host-only permissions are denied for every user.
/// </summary>
public class RolePermissions_Tests : BlogApplicationTestBase
{
    private readonly UserManager _userManager;
    private readonly IPermissionManager _permissionManager;

    public RolePermissions_Tests()
    {
        _userManager = Resolve<UserManager>();
        _permissionManager = Resolve<IPermissionManager>();
    }

    [Fact]
    public void Permissions_Should_Be_Side_Agnostic()
    {
        var allPermissionNames = new[]
        {
            PermissionNames.Pages_Users,
            PermissionNames.Pages_Users_Activation,
            PermissionNames.Pages_Roles,
            PermissionNames.Blog.Posts_Create,
            PermissionNames.Blog.Posts_Edit,
            PermissionNames.Blog.Posts_Delete,
            PermissionNames.Blog.Posts_Archive,
            PermissionNames.Blog.Posts_Approve,
            PermissionNames.Blog.Comments_Create,
            PermissionNames.Blog.Comments_Edit,
            PermissionNames.Blog.Comments_Delete,
            PermissionNames.Blog.Replies_Create,
            PermissionNames.Blog.Upvotes_Toggle,
            PermissionNames.Blog.Bans_Manage
        };

        foreach (var permissionName in allPermissionNames)
        {
            var permission = _permissionManager.GetPermission(permissionName);
            permission.MultiTenancySides.HasFlag(MultiTenancySides.Tenant).ShouldBeTrue(
                $"{permissionName} must include the Tenant side - a tenancy-disabled session never resolves to the Host side");
        }
    }

    [Fact]
    public async Task Admin_Should_Be_Granted_User_And_Role_Management()
    {
        var admin = await GetCurrentUserAsync();

        (await _userManager.IsGrantedAsync(admin.Id, PermissionNames.Pages_Users)).ShouldBeTrue();
        (await _userManager.IsGrantedAsync(admin.Id, PermissionNames.Pages_Roles)).ShouldBeTrue();
    }

    [Fact]
    public async Task Author_Role_Should_Have_Post_But_Not_Approval_Permissions()
    {
        var author = await CreateUserInRoleAsync(StaticRoleNames.Host.Author);

        (await _userManager.IsGrantedAsync(author.Id, PermissionNames.Blog.Posts_Create)).ShouldBeTrue();
        (await _userManager.IsGrantedAsync(author.Id, PermissionNames.Blog.Comments_Create)).ShouldBeTrue();
        (await _userManager.IsGrantedAsync(author.Id, PermissionNames.Blog.Posts_Approve)).ShouldBeFalse();
        (await _userManager.IsGrantedAsync(author.Id, PermissionNames.Blog.Bans_Manage)).ShouldBeFalse();
    }

    private async Task<User> CreateUserInRoleAsync(string roleName)
    {
        var user = new User
        {
            TenantId = null,
            UserName = "role-permissions.user",
            Name = "RolePermissions",
            Surname = "Test",
            EmailAddress = "role-permissions.user@blogapplication.com",
            IsActive = true,
            IsEmailConfirmed = true
        };

        user.SetNormalizedNames();

        await _userManager.InitializeOptionsAsync(null);
        (await _userManager.CreateAsync(user, "123qwe")).Succeeded.ShouldBeTrue();
        (await _userManager.AddToRoleAsync(user, roleName)).Succeeded.ShouldBeTrue();

        return user;
    }
}
