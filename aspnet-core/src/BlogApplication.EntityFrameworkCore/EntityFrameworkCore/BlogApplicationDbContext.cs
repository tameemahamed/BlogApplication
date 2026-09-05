using Abp.Zero.EntityFrameworkCore;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.MultiTenancy;
using BlogApplication.Posts;
using Microsoft.EntityFrameworkCore;

namespace BlogApplication.EntityFrameworkCore;

public class BlogApplicationDbContext : AbpZeroDbContext<Tenant, Role, User, BlogApplicationDbContext>
{
    /* Define a DbSet for each entity of the application */

    public DbSet<Post> Posts { get; set; }

    public BlogApplicationDbContext(DbContextOptions<BlogApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Post>(b =>
        {
            b.ToTable("Posts");
            b.Property(p => p.Title).IsRequired().HasMaxLength(PostConsts.MaxTitleLength);
            b.Property(p => p.Slug).IsRequired().HasMaxLength(PostConsts.MaxSlugLength);
            b.Property(p => p.Excerpt).HasMaxLength(PostConsts.MaxExcerptLength);
            b.Property(p => p.ContentMarkdown).IsRequired();
            b.Property(p => p.RejectionReason).HasMaxLength(PostConsts.MaxRejectionReasonLength);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Partial indexes: soft-deleted posts release their slug and stay
            // out of the author and status indexes (prd.md D7/D8)
            b.HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("UX_Posts_Slug")
                .HasFilter("\"IsDeleted\" = false");
            b.HasIndex(p => p.AuthorId)
                .HasDatabaseName("IX_Posts_AuthorId")
                .HasFilter("\"IsDeleted\" = false");
            b.HasIndex(p => new { p.Status, p.PublishedAt })
                .HasDatabaseName("IX_Posts_Status_PublishedAt")
                .HasFilter("\"IsDeleted\" = false");
        });
    }
}
