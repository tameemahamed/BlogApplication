using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BlogApplication.EntityFrameworkCore;

public static class BlogApplicationDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<BlogApplicationDbContext> builder, string connectionString)
    {
        builder.UseNpgsql(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<BlogApplicationDbContext> builder, DbConnection connection)
    {
        builder.UseNpgsql(connection);
    }
}
