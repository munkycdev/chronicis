using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Data
{
    /// <summary>
    /// Database context for Chronicis application.
    /// </summary>
    public class ChronicisDbContext : DbContext
    {
        public DbSet<Hashtag> Hashtags { get; set; } = null!;
        public DbSet<ArticleHashtag> ArticleHashtags { get; set; } = null!;
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        public ChronicisDbContext(DbContextOptions<ChronicisDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Article configuration
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(a => a.Id);

                // Title configuration
                entity.Property(a => a.Title)
                    .HasMaxLength(500);

                // Slug configuration
                entity.Property(a => a.Slug)
                    .HasMaxLength(200)
                    .IsRequired();

                // Self-referencing hierarchy
                entity.HasOne(a => a.Parent)
                    .WithMany(a => a.Children)
                    .HasForeignKey(a => a.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.User)
                  .WithMany(u => u.Articles)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

                // Indexes
                entity.HasIndex(a => a.ParentId);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.Title);
                
                // Unique constraint: Slug must be unique within parent scope
                // For root articles (ParentId is null)
                entity.HasIndex(a => a.Slug)
                    .IsUnique()
                    .HasFilter("[ParentId] IS NULL")
                    .HasDatabaseName("IX_Articles_Slug_Root");
                
                // For child articles (ParentId is not null)
                entity.HasIndex(a => new { a.ParentId, a.Slug })
                    .IsUnique()
                    .HasFilter("[ParentId] IS NOT NULL")
                    .HasDatabaseName("IX_Articles_ParentId_Slug");
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Auth0UserId)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(u => u.Email)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(u => u.DisplayName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(u => u.Auth0UserId)
                    .IsUnique();
            });

            // Hashtag configuration
            modelBuilder.Entity<Hashtag>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasOne(h => h.LinkedArticle)
                    .WithMany()
                    .HasForeignKey(h => h.LinkedArticleId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(h => h.Name)
                    .IsUnique();
            });

            // ArticleHashtag (junction table) configuration
            modelBuilder.Entity<ArticleHashtag>(entity =>
            {
                entity.HasKey(ah => ah.Id);

                entity.HasOne(ah => ah.Article)
                    .WithMany(a => a.ArticleHashtags)
                    .HasForeignKey(ah => ah.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ah => ah.Hashtag)
                    .WithMany(h => h.ArticleHashtags)
                    .HasForeignKey(ah => ah.HashtagId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(ah => new { ah.ArticleId, ah.HashtagId })
                    .IsUnique();
            });
        }
    }
}
