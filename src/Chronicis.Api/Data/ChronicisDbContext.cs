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
            base.OnModelCreating(modelBuilder);

            // Configure self-referencing hierarchy
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Title)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(a => a.Body)
                    .IsRequired(false);

                entity.Property(a => a.CreatedDate)
                    .IsRequired();

                // Self-referencing relationship
                entity.HasOne(a => a.Parent)
                    .WithMany(a => a.Children)
                    .HasForeignKey(a => a.ParentId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent cascade deletes

                // Index for performance
                entity.HasIndex(a => a.ParentId);
                entity.HasIndex(a => a.Title);
            });

            // Hashtag configuration
            modelBuilder.Entity<Hashtag>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                // Create unique index on Name (case-insensitive)
                entity.HasIndex(h => h.Name)
                    .IsUnique();

                entity.Property(h => h.CreatedDate)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Optional foreign key to linked article
                entity.HasOne(h => h.LinkedArticle)
                    .WithMany()
                    .HasForeignKey(h => h.LinkedArticleId)
                    .OnDelete(DeleteBehavior.SetNull); // If article deleted, unlink hashtag
            });

            // ArticleHashtag junction configuration
            modelBuilder.Entity<ArticleHashtag>(entity =>
            {
                entity.HasKey(ah => ah.Id);

                // Foreign key to Article
                entity.HasOne(ah => ah.Article)
                    .WithMany(a => a.ArticleHashtags)
                    .HasForeignKey(ah => ah.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete hashtag references when article deleted

                // Foreign key to Hashtag
                entity.HasOne(ah => ah.Hashtag)
                    .WithMany(h => h.ArticleHashtags)
                    .HasForeignKey(ah => ah.HashtagId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete references when hashtag deleted

                // Composite index for performance
                entity.HasIndex(ah => new { ah.ArticleId, ah.HashtagId });

                entity.Property(ah => ah.Position)
                    .IsRequired();

                entity.Property(ah => ah.CreatedDate)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Auth0UserId).IsUnique();
                entity.Property(u => u.Auth0UserId).IsRequired().HasMaxLength(256);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                entity.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            });

            // Article-User relationship
            modelBuilder.Entity<Article>()
                .HasOne(a => a.User)
                .WithMany(u => u.Articles)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed data for development
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Use static dates instead of DateTime.UtcNow
            var baseDate = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

            // Root articles
            modelBuilder.Entity<Article>().HasData(
                new Article
                {
                    Id = 1,
                    Title = "World",
                    Body = "Overview of the campaign world.",
                    CreatedDate = baseDate
                },
                new Article
                {
                    Id = 2,
                    Title = "Characters",
                    Body = "Player characters and important NPCs.",
                    CreatedDate = baseDate
                },
                new Article
                {
                    Id = 3,
                    Title = "Sessions",
                    Body = "Campaign session notes and history.",
                    CreatedDate = baseDate
                }
            );

            // Child articles under World
            modelBuilder.Entity<Article>().HasData(
                new Article
                {
                    Id = 4,
                    Title = "Sword Coast",
                    ParentId = 1,
                    Body = "The western region of Faerûn.",
                    CreatedDate = baseDate.AddDays(30)
                },
                new Article
                {
                    Id = 5,
                    Title = "Waterdeep",
                    ParentId = 4,
                    Body = "The City of Splendors, largest city on the Sword Coast.",
                    CreatedDate = baseDate.AddDays(60)
                }
            );

            // Child articles under Characters
            modelBuilder.Entity<Article>().HasData(
                new Article
                {
                    Id = 6,
                    Title = "Thorin Ironforge",
                    ParentId = 2,
                    Body = "Dwarf fighter, member of the adventuring party.",
                    CreatedDate = baseDate.AddDays(90)
                },
                new Article
                {
                    Id = 7,
                    Title = "Elara Moonwhisper",
                    ParentId = 2,
                    Body = "Elf wizard, specializes in divination magic.",
                    CreatedDate = baseDate.AddDays(90)
                }
            );

            // Child articles under Sessions
            modelBuilder.Entity<Article>().HasData(
                new Article
                {
                    Id = 8,
                    Title = "Session 1: The Adventure Begins",
                    ParentId = 3,
                    Body = "The party meets at the Yawning Portal tavern in Waterdeep.",
                    CreatedDate = baseDate.AddDays(120)
                }
            );
        }

    }
}