using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Data
{
    /// <summary>
    /// Database context for Chronicis application.
    /// </summary>
    public class ChronicisDbContext : DbContext
    {
        public ChronicisDbContext(DbContextOptions<ChronicisDbContext> options)
            : base(options)
        {
        }

        public DbSet<Article> Articles { get; set; } = null!;

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

            // Seed data for development
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Root articles
            modelBuilder.Entity<Article>().HasData(
                new Article
                {
                    Id = 1,
                    Title = "World",
                    Body = "Overview of the campaign world.",
                    CreatedDate = DateTime.UtcNow.AddMonths(-6)
                },
                new Article
                {
                    Id = 2,
                    Title = "Characters",
                    Body = "Player characters and important NPCs.",
                    CreatedDate = DateTime.UtcNow.AddMonths(-6)
                },
                new Article
                {
                    Id = 3,
                    Title = "Sessions",
                    Body = "Campaign session notes and history.",
                    CreatedDate = DateTime.UtcNow.AddMonths(-6)
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
                    CreatedDate = DateTime.UtcNow.AddMonths(-5)
                },
                new Article
                {
                    Id = 5,
                    Title = "Waterdeep",
                    ParentId = 4,
                    Body = "The City of Splendors, largest city on the Sword Coast.",
                    CreatedDate = DateTime.UtcNow.AddMonths(-5)
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
                    CreatedDate = DateTime.UtcNow.AddMonths(-4)
                },
                new Article
                {
                    Id = 7,
                    Title = "Elara Moonwhisper",
                    ParentId = 2,
                    Body = "Elf wizard, specializes in divination magic.",
                    CreatedDate = DateTime.UtcNow.AddMonths(-4)
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
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                }
            );
        }
    }
}
