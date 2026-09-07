using Abp.Zero.EntityFrameworkCore;
using BlogApplication.Authorization.Roles;
using BlogApplication.Authorization.Users;
using BlogApplication.Comments;
using BlogApplication.MultiTenancy;
using BlogApplication.Posts;
using BlogApplication.Upvotes;
using Microsoft.EntityFrameworkCore;

namespace BlogApplication.EntityFrameworkCore;

public class BlogApplicationDbContext : AbpZeroDbContext<Tenant, Role, User, BlogApplicationDbContext>
{
    /* Define a DbSet for each entity of the application */

    public DbSet<Post> Posts { get; set; }

    public DbSet<Comment> Comments { get; set; }

    public DbSet<Upvote> Upvotes { get; set; }

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

        builder.Entity<Comment>(b =>
        {
            b.ToTable("Comments");
            b.Property(c => c.ContentMarkdown).IsRequired().HasMaxLength(CommentConsts.MaxContentLength);

            b.HasOne<Post>()
                .WithMany()
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Comment>()
                .WithMany()
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(c => new { c.PostId, c.ParentCommentId })
                .HasDatabaseName("IX_Comments_PostId_ParentCommentId")
                .HasFilter("\"IsDeleted\" = false");
            b.HasIndex(c => c.ParentCommentId)
                .HasDatabaseName("IX_Comments_ParentCommentId")
                .HasFilter("\"IsDeleted\" = false");
            b.HasIndex(c => c.UserId)
                .HasDatabaseName("IX_Comments_UserId")
                .HasFilter("\"IsDeleted\" = false");
            b.HasIndex(c => c.CreationTime)
                .HasDatabaseName("IX_Comments_CreationTime");
        });

        builder.Entity<Upvote>(b =>
        {
            b.ToTable("Upvotes");

            // No soft-delete filter: a toggled-off upvote is physically deleted,
            // so the plain unique index frees the slot for a re-upvote (prd.md D3/D4)
            b.HasIndex(v => new { v.UserId, v.TargetType, v.TargetId })
                .IsUnique()
                .HasDatabaseName("UX_Upvotes_UserId_TargetType_TargetId");
            b.HasIndex(v => new { v.TargetType, v.TargetId })
                .HasDatabaseName("IX_Upvotes_TargetType_TargetId");

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
