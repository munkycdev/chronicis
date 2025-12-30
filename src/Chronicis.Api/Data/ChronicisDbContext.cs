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
    public DbSet<Arc> Arcs { get; set; } = null!;
    public DbSet<Article> Articles { get; set; } = null!;
    public DbSet<ArticleLink> ArticleLinks { get; set; } = null!;
    public DbSet<WorldLink> WorldLinks { get; set; } = null!;
    public DbSet<SummaryTemplate> SummaryTemplates { get; set; } = null!;


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
        ConfigureArc(modelBuilder);
        ConfigureArticle(modelBuilder);
        ConfigureArticleLink(modelBuilder);
        ConfigureWorldLink(modelBuilder);
        ConfigureSummaryTemplate(modelBuilder);
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

            entity.Property(w => w.Slug)
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

            // Unique constraint: Slug must be unique per owner
            entity.HasIndex(w => new { w.OwnerId, w.Slug })
                .IsUnique()
                .HasDatabaseName("IX_Worlds_OwnerId_Slug");
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

    private static void ConfigureArc(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Arc>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.Description)
                .HasMaxLength(1000);

            // Arc -> Campaign
            entity.HasOne(a => a.Campaign)
                .WithMany(c => c.Arcs)
                .HasForeignKey(a => a.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            // Arc -> Creator (User)
            entity.HasOne(a => a.Creator)
                .WithMany(u => u.CreatedArcs)
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(a => a.CampaignId);
            entity.HasIndex(a => a.CreatedBy);
            entity.HasIndex(a => new { a.CampaignId, a.SortOrder });
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

            // Article -> Arc (for Session articles)
            entity.HasOne(a => a.Arc)
                .WithMany(arc => arc.Sessions)
                .HasForeignKey(a => a.ArcId)
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

            // Unique constraint: Slug must be unique among siblings
            // For root articles (ParentId is null), scope by WorldId
            entity.HasIndex(a => new { a.WorldId, a.Slug })
                .IsUnique()
                .HasFilter("[ParentId] IS NULL")
                .HasDatabaseName("IX_Articles_WorldId_Slug_Root");

            // For child articles (ParentId is not null)
            entity.HasIndex(a => new { a.ParentId, a.Slug })
                .IsUnique()
                .HasFilter("[ParentId] IS NOT NULL")
                .HasDatabaseName("IX_Articles_ParentId_Slug");
        });
    }

    private static void ConfigureArticleLink(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleLink>(entity =>
        {
            entity.HasKey(al => al.Id);

            // DisplayText max length
            entity.Property(al => al.DisplayText)
                .HasMaxLength(500);

            // ArticleLink -> SourceArticle (CASCADE delete - when source is deleted, remove its links)
            entity.HasOne(al => al.SourceArticle)
                .WithMany(a => a.OutgoingLinks)
                .HasForeignKey(al => al.SourceArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            // ArticleLink -> TargetArticle (NO ACTION - SQL Server limitation with multiple cascade paths)
            // When target article is deleted, links must be cleaned up manually or via triggers
            entity.HasOne(al => al.TargetArticle)
                .WithMany(a => a.IncomingLinks)
                .HasForeignKey(al => al.TargetArticleId)
                .OnDelete(DeleteBehavior.NoAction);

            // Indexes for query performance
            entity.HasIndex(al => al.SourceArticleId);
            entity.HasIndex(al => al.TargetArticleId);

            // Unique constraint: Prevent duplicate links at same position
            entity.HasIndex(al => new { al.SourceArticleId, al.TargetArticleId, al.Position })
                .IsUnique();
        });
    }

    private static void ConfigureWorldLink(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldLink>(entity =>
        {
            entity.HasKey(wl => wl.Id);

            entity.Property(wl => wl.Url)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(wl => wl.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(wl => wl.Description)
                .HasMaxLength(500);

            // WorldLink -> World (CASCADE delete - when world is deleted, remove its links)
            entity.HasOne(wl => wl.World)
                .WithMany(w => w.Links)
                .HasForeignKey(wl => wl.WorldId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for query performance
            entity.HasIndex(wl => wl.WorldId);
        });
    }

    private static void ConfigureSummaryTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SummaryTemplate>(entity =>
        {
            entity.HasKey(st => st.Id);

            entity.Property(st => st.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(st => st.Description)
                .HasMaxLength(500);

            entity.Property(st => st.PromptTemplate)
                .IsRequired();

            // SummaryTemplate -> World (optional, for future world-specific templates)
            entity.HasOne(st => st.World)
                .WithMany()
                .HasForeignKey(st => st.WorldId)
                .OnDelete(DeleteBehavior.Cascade);

            // SummaryTemplate -> Creator (optional, for future user-created templates)
            entity.HasOne(st => st.Creator)
                .WithMany()
                .HasForeignKey(st => st.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(st => st.WorldId);
            entity.HasIndex(st => st.IsSystem);
        });

        // Article -> SummaryTemplate relationship
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasOne(a => a.SummaryTemplate)
                .WithMany()
                .HasForeignKey(a => a.SummaryTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Campaign -> SummaryTemplate relationship
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasOne(c => c.SummaryTemplate)
                .WithMany()
                .HasForeignKey(c => c.SummaryTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Arc -> SummaryTemplate relationship
        modelBuilder.Entity<Arc>(entity =>
        {
            entity.HasOne(a => a.SummaryTemplate)
                .WithMany()
                .HasForeignKey(a => a.SummaryTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
