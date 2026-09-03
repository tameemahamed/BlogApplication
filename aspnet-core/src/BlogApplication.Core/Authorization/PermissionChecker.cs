using Abp.Authorization;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;

namespace BlogApplication.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}
