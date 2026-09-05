using Abp.Authorization.Users;
using Abp.Domain.Services;
using Abp.IdentityFramework;
using BlogApplication.Authorization.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogApplication.Authorization.Users;

public class UserRegistrationManager : DomainService
{
    private readonly UserManager _userManager;
    private readonly RoleManager _roleManager;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserRegistrationManager(
        UserManager userManager,
        RoleManager roleManager,
        IPasswordHasher<User> passwordHasher)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> RegisterAsync(string name, string surname, string emailAddress, string userName, string plainPassword, bool isEmailConfirmed)
    {
        // Single-tenant application: every registered user is a host user.
        var user = new User
        {
            TenantId = null,
            Name = name,
            Surname = surname,
            EmailAddress = emailAddress,
            IsActive = true,
            UserName = userName,
            IsEmailConfirmed = isEmailConfirmed,
            Roles = new List<UserRole>()
        };

        user.SetNormalizedNames();

        foreach (var defaultRole in await _roleManager.Roles.Where(r => r.IsDefault).ToListAsync())
        {
            user.Roles.Add(new UserRole(null, user.Id, defaultRole.Id));
        }

        await _userManager.InitializeOptionsAsync(null);

        CheckErrors(await _userManager.CreateAsync(user, plainPassword));
        await CurrentUnitOfWork.SaveChangesAsync();

        return user;
    }

    protected virtual void CheckErrors(IdentityResult identityResult)
    {
        identityResult.CheckErrors(LocalizationManager);
    }
}
