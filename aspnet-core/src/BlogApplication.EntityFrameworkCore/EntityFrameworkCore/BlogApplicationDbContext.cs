using Abp.Zero.EntityFrameworkCore;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace BlogApplication.EntityFrameworkCore;

public class BlogApplicationDbContext : AbpZeroDbContext<Tenant, Role, User, BlogApplicationDbContext>
{
    /* Define a DbSet for each entity of the application */

    public BlogApplicationDbContext(DbContextOptions<BlogApplicationDbContext> options)
        : base(options)
    {
    }
}
