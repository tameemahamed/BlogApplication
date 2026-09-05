using Abp.Authorization;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace BlogApplication.Tests.Authorization;

/// <summary>
/// Verifies the framework mechanism bans are built on (prd.md E1-S7): a user-level
/// permission prohibition is evaluated before role grants and therefore wins.
/// </summary>
public class PermissionProhibition_Tests : BlogApplicationTestBase
{
    private readonly UserManager _userManager;
    private readonly IPermissionManager _permissionManager;

    public PermissionProhibition_Tests()
    {
        _userManager = Resolve<UserManager>();
        _permissionManager = Resolve<IPermissionManager>();
    }

    [Fact]
    public async Task User_Level_Prohibition_Should_Override_Role_Grant()
    {
        // Arrange - the seeded User role grants the community permissions
        var user = await CreateUserInRoleAsync(StaticRoleNames.Host.User);

        (await _userManager.IsGrantedAsync(user.Id, PermissionNames.Blog.Comments_Create)).ShouldBeTrue();

        // Act - impose a ban: user-level prohibition
        await _userManager.ProhibitPermissionAsync(
            user,
            _permissionManager.GetPermission(PermissionNames.Blog.Comments_Create));

        // Assert - the prohibition is evaluated before role grants, so the ban wins
        (await _userManager.IsGrantedAsync(user.Id, PermissionNames.Blog.Comments_Create)).ShouldBeFalse();

        // And it is granular: unrelated permissions keep working
        (await _userManager.IsGrantedAsync(user.Id, PermissionNames.Blog.Upvotes_Toggle)).ShouldBeTrue();
    }

    private async Task<User> CreateUserInRoleAsync(string roleName)
    {
        var user = new User
        {
            TenantId = null,
            UserName = "prohibition.user",
            Name = "Prohibition",
            Surname = "Test",
            EmailAddress = "prohibition.user@blogapplication.com",
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
