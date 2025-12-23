using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Data;

/// <summary>
/// Database context for Chronicis application.
/// </summary>
public class ChronicisDbContext : DbContext
{
    // ===== Core Entities =====
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<World> Worlds { get; set; } = null!;
    public DbSet<Campaign> Campaigns { get; set; } = null!;
    public DbSet<CampaignMember> CampaignMembers { get; set; } = null!;
    public DbSet<Article> Articles { get; set; } = null!;
    


    public ChronicisDbContext(DbContextOptions<ChronicisDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureWorld(modelBuilder);
        ConfigureCampaign(modelBuilder);
        ConfigureCampaignMember(modelBuilder);
        ConfigureArticle(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
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

            entity.Property(u => u.AvatarUrl)
                .HasMaxLength(500);

            entity.HasIndex(u => u.Auth0UserId)
                .IsUnique();

            entity.HasIndex(u => u.Email);
        });
    }

    private static void ConfigureWorld(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<World>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.Property(w => w.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(w => w.Description)
                .HasMaxLength(1000);

            // World -> Owner (User)
            entity.HasOne(w => w.Owner)
                .WithMany(u => u.OwnedWorlds)
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(w => w.OwnerId);
        });
    }

    private static void ConfigureCampaign(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(1000);

            // Campaign -> World
            entity.HasOne(c => c.World)
                .WithMany(w => w.Campaigns)
                .HasForeignKey(c => c.WorldId)
                .OnDelete(DeleteBehavior.Restrict);

            // Campaign -> Owner (User)
            entity.HasOne(c => c.Owner)
                .WithMany(u => u.OwnedCampaigns)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.WorldId);
            entity.HasIndex(c => c.OwnerId);
        });
    }

    private static void ConfigureCampaignMember(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CampaignMember>(entity =>
        {
            entity.HasKey(cm => cm.Id);

            entity.Property(cm => cm.CharacterName)
                .HasMaxLength(100);

            // CampaignMember -> Campaign
            entity.HasOne(cm => cm.Campaign)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            // CampaignMember -> User
            entity.HasOne(cm => cm.User)
                .WithMany(u => u.CampaignMemberships)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: One membership per user per campaign
            entity.HasIndex(cm => new { cm.CampaignId, cm.UserId })
                .IsUnique();
        });
    }

    private static void ConfigureArticle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(a => a.Id);

            // Content fields
            entity.Property(a => a.Title)
                .HasMaxLength(500);

            entity.Property(a => a.Slug)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.IconEmoji)
                .HasMaxLength(50);

            entity.Property(a => a.InGameDate)
                .HasMaxLength(100);

            // Self-referencing hierarchy
            entity.HasOne(a => a.Parent)
                .WithMany(a => a.Children)
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Article -> World
            entity.HasOne(a => a.World)
                .WithMany(w => w.Articles)
                .HasForeignKey(a => a.WorldId)
                .OnDelete(DeleteBehavior.Restrict);

            // Article -> Campaign
            entity.HasOne(a => a.Campaign)
                .WithMany(c => c.Articles)
                .HasForeignKey(a => a.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            // Article -> Creator (User)
            entity.HasOne(a => a.Creator)
                .WithMany(u => u.CreatedArticles)
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Article -> Modifier (User)
            entity.HasOne(a => a.Modifier)
                .WithMany(u => u.ModifiedArticles)
                .HasForeignKey(a => a.LastModifiedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Article -> Player (User) for Character ownership
            entity.HasOne(a => a.Player)
                .WithMany(u => u.OwnedCharacters)
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(a => a.ParentId);
            entity.HasIndex(a => a.WorldId);
            entity.HasIndex(a => a.CampaignId);
            entity.HasIndex(a => a.CreatedBy);
            entity.HasIndex(a => a.Type);
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
    }


}
