using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Data;

/// <summary>
/// Database context for Chronicis application.
/// </summary>
[ExcludeFromCodeCoverage]
public class ChronicisDbContext : DbContext
{
    // ===== Core Entities =====
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<World> Worlds { get; set; } = null!;
    public DbSet<WorldMember> WorldMembers { get; set; } = null!;
    public DbSet<WorldInvitation> WorldInvitations { get; set; } = null!;
    public DbSet<Campaign> Campaigns { get; set; } = null!;
    public DbSet<Arc> Arcs { get; set; } = null!;
    public DbSet<Article> Articles { get; set; } = null!;
    public DbSet<TutorialPage> TutorialPages { get; set; } = null!;
    public DbSet<ArticleAlias> ArticleAliases { get; set; } = null!;
    public DbSet<ArticleLink> ArticleLinks { get; set; } = null!;
    public DbSet<ArticleExternalLink> ArticleExternalLinks { get; set; } = null!;
    public DbSet<WorldLink> WorldLinks { get; set; } = null!;
    public DbSet<WorldDocument> WorldDocuments { get; set; } = null!;
    public DbSet<SummaryTemplate> SummaryTemplates { get; set; } = null!;
    public DbSet<ResourceProvider> ResourceProviders { get; set; } = null!;
    public DbSet<WorldResourceProvider> WorldResourceProviders { get; set; } = null!;
    public DbSet<Quest> Quests { get; set; } = null!;
    public DbSet<QuestUpdate> QuestUpdates { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;


    public ChronicisDbContext(DbContextOptions<ChronicisDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder);
        ConfigureWorld(modelBuilder);
        ConfigureWorldMember(modelBuilder);
        ConfigureWorldInvitation(modelBuilder);
        ConfigureCampaign(modelBuilder);
        ConfigureArc(modelBuilder);
        ConfigureArticle(modelBuilder);
        ConfigureTutorialPage(modelBuilder);
        ConfigureArticleAlias(modelBuilder);
        ConfigureArticleLink(modelBuilder);
        ConfigureArticleExternalLink(modelBuilder);
        ConfigureWorldLink(modelBuilder);
        ConfigureWorldDocument(modelBuilder);
        ConfigureSummaryTemplate(modelBuilder);
        ConfigureResourceProvider(modelBuilder);
        ConfigureWorldResourceProvider(modelBuilder);
        ConfigureQuest(modelBuilder);
        ConfigureQuestUpdate(modelBuilder);
        ConfigureSession(modelBuilder);
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

            entity.Property(w => w.IsTutorial)
                .HasDefaultValue(false);

            // Public access fields
            entity.Property(w => w.IsPublic)
                .HasDefaultValue(false);

            entity.Property(w => w.PublicSlug)
                .HasMaxLength(100);

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

            // Unique constraint: PublicSlug must be globally unique (when not null)
            entity.HasIndex(w => w.PublicSlug)
                .IsUnique()
                .HasFilter("[PublicSlug] IS NOT NULL")
                .HasDatabaseName("IX_Worlds_PublicSlug");
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

    private static void ConfigureWorldMember(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldMember>(entity =>
        {
            entity.HasKey(wm => wm.Id);

            // WorldMember -> World
            entity.HasOne(wm => wm.World)
                .WithMany(w => w.Members)
                .HasForeignKey(wm => wm.WorldId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorldMember -> User
            entity.HasOne(wm => wm.User)
                .WithMany(u => u.WorldMemberships)
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorldMember -> Inviter (User)
            entity.HasOne(wm => wm.Inviter)
                .WithMany(u => u.InvitedMembers)
                .HasForeignKey(wm => wm.InvitedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: One membership per user per world
            entity.HasIndex(wm => new { wm.WorldId, wm.UserId })
                .IsUnique();
        });
    }

    private static void ConfigureWorldInvitation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldInvitation>(entity =>
        {
            entity.HasKey(wi => wi.Id);

            entity.Property(wi => wi.Code)
                .HasMaxLength(9) // XXXX-XXXX format
                .IsRequired();

            // WorldInvitation -> World
            entity.HasOne(wi => wi.World)
                .WithMany(w => w.Invitations)
                .HasForeignKey(wi => wi.WorldId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorldInvitation -> Creator (User)
            entity.HasOne(wi => wi.Creator)
                .WithMany(u => u.CreatedInvitations)
                .HasForeignKey(wi => wi.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: Invitation codes must be globally unique
            entity.HasIndex(wi => wi.Code)
                .IsUnique();

            // Index for looking up active invitations by world
            entity.HasIndex(wi => new { wi.WorldId, wi.IsActive });
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

            // Article -> Arc (for Session articles) — uses renamed nav property SessionArticles
            entity.HasOne(a => a.Arc)
                .WithMany(arc => arc.SessionArticles)
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
            entity.HasIndex(a => a.SessionId)
                .HasDatabaseName("IX_Articles_SessionId")
                .HasFilter("[SessionId] IS NOT NULL");

            // Article -> Session entity (nullable FK for SessionNote articles)
            entity.HasOne(a => a.Session)
                .WithMany(s => s.SessionNotes)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.SetNull);

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

    private static void ConfigureTutorialPage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TutorialPage>(entity =>
        {
            entity.HasKey(tp => tp.Id);

            entity.Property(tp => tp.PageType)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(tp => tp.PageTypeName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(tp => tp.CreatedAt)
                .IsRequired();

            entity.Property(tp => tp.ModifiedAt)
                .IsRequired();

            entity.HasOne(tp => tp.Article)
                .WithMany()
                .HasForeignKey(tp => tp.ArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(tp => tp.PageType)
                .IsUnique();

            entity.HasIndex(tp => tp.ArticleId);
        });
    }

    private static void ConfigureArticleAlias(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleAlias>(entity =>
        {
            entity.HasKey(aa => aa.Id);

            // AliasText is required, max 200 characters
            entity.Property(aa => aa.AliasText)
                .HasMaxLength(200)
                .IsRequired();

            // AliasType is optional (for future use)
            entity.Property(aa => aa.AliasType)
                .HasMaxLength(50);

            // ArticleAlias -> Article (CASCADE delete - when article is deleted, remove its aliases)
            entity.HasOne(aa => aa.Article)
                .WithMany(a => a.Aliases)
                .HasForeignKey(aa => aa.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for looking up aliases by article
            entity.HasIndex(aa => aa.ArticleId);

            // Index for searching aliases (case-insensitive search will be handled in queries)
            entity.HasIndex(aa => aa.AliasText);

            // Unique constraint: No duplicate aliases on the same article
            entity.HasIndex(aa => new { aa.ArticleId, aa.AliasText })
                .IsUnique()
                .HasDatabaseName("IX_ArticleAliases_ArticleId_AliasText");
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

    private static void ConfigureArticleExternalLink(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleExternalLink>(entity =>
        {
            entity.HasKey(ael => ael.Id);

            // String field max lengths
            entity.Property(ael => ael.Source)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(ael => ael.ExternalId)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(ael => ael.DisplayTitle)
                .HasMaxLength(500)
                .IsRequired();

            // ArticleExternalLink -> Article (CASCADE delete - when article is deleted, remove its external links)
            entity.HasOne(ael => ael.Article)
                .WithMany(a => a.ExternalLinks)
                .HasForeignKey(ael => ael.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for query performance (lookup all external links for an article)
            entity.HasIndex(ael => ael.ArticleId);

            // Composite index for uniqueness and query performance
            // An article should not have duplicate references to the same external resource
            entity.HasIndex(ael => new { ael.ArticleId, ael.Source, ael.ExternalId })
                .IsUnique()
                .HasDatabaseName("IX_ArticleExternalLinks_ArticleId_Source_ExternalId");
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

    private static void ConfigureWorldDocument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldDocument>(entity =>
        {
            entity.HasKey(wd => wd.Id);

            entity.Property(wd => wd.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(wd => wd.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(wd => wd.BlobPath)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(wd => wd.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(wd => wd.Description)
                .HasMaxLength(500);

            // WorldDocument -> World (CASCADE delete - when world is deleted, remove its documents)
            entity.HasOne(wd => wd.World)
                .WithMany(w => w.Documents)
                .HasForeignKey(wd => wd.WorldId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorldDocument -> Article (SET NULL - when article is deleted, preserve document but clear reference)
            entity.HasOne(wd => wd.Article)
                .WithMany(a => a.Images)
                .HasForeignKey(wd => wd.ArticleId)
                .OnDelete(DeleteBehavior.SetNull);

            // WorldDocument -> UploadedBy (User)
            entity.HasOne(wd => wd.UploadedBy)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(wd => wd.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for query performance
            entity.HasIndex(wd => wd.WorldId);
            entity.HasIndex(wd => wd.UploadedById);
            entity.HasIndex(wd => wd.ArticleId)
                .HasFilter("[ArticleId] IS NOT NULL")
                .HasDatabaseName("IX_WorldDocuments_ArticleId");
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

    private static void ConfigureResourceProvider(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResourceProvider>(entity =>
        {
            // Primary key is Code (string)
            entity.HasKey(rp => rp.Code);

            entity.Property(rp => rp.Code)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(rp => rp.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(rp => rp.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(rp => rp.DocumentationLink)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(rp => rp.License)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(rp => rp.IsActive)
                .HasDefaultValue(true);

            entity.Property(rp => rp.CreatedAt)
                .IsRequired();

            // Seed initial providers
            entity.HasData(
                new ResourceProvider
                {
                    Code = "srd",
                    Name = "Open 5e API",
                    Description = "System Reference Document for D&D 5th Edition",
                    DocumentationLink = "https://open5e.com/api-docs",
                    License = "https://open5e.com/legal",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new ResourceProvider
                {
                    Code = "srd14",
                    Name = "SRD 2014",
                    Description = "System Reference Document 5.1",
                    DocumentationLink = "https://www.dndbeyond.com/srd?srsltid=AfmBOooZgD0uD_hbmyYkHEvFJtDJzktTdIa_J_N2GRnkPQvGIZ4ZSeBO#SystemReferenceDocumentv51",
                    License = "https://opengamingfoundation.org/ogl.html",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new ResourceProvider
                {
                    Code = "srd24",
                    Name = "SRD 2024",
                    Description = "System Reference Document 5.2.1",
                    DocumentationLink = "https://www.dndbeyond.com/srd?srsltid=AfmBOooZgD0uD_hbmyYkHEvFJtDJzktTdIa_J_N2GRnkPQvGIZ4ZSeBO#SystemReferenceDocumentv52",
                    License = "https://creativecommons.org/licenses/by/4.0/",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new ResourceProvider
                {
                    Code = "ros",
                    Name = "Ruins of Symbaroum",
                    Description = "Ruins of Symbaroum source material",
                    DocumentationLink = "https://freeleaguepublishing.com/games/ruins-of-symbaroum/",
                    License = "https://opengamingfoundation.org/ogl.html",
                    IsActive = true,
                    CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                }
            );
        });
    }

    private static void ConfigureWorldResourceProvider(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldResourceProvider>(entity =>
        {
            // Composite primary key
            entity.HasKey(wrp => new { wrp.WorldId, wrp.ResourceProviderCode });

            entity.Property(wrp => wrp.IsEnabled)
                .IsRequired();

            entity.Property(wrp => wrp.ModifiedAt)
                .IsRequired();

            entity.Property(wrp => wrp.ModifiedByUserId)
                .IsRequired();

            // WorldResourceProvider -> World (CASCADE delete - when world is deleted, remove provider associations)
            entity.HasOne(wrp => wrp.World)
                .WithMany(w => w.WorldResourceProviders)
                .HasForeignKey(wrp => wrp.WorldId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorldResourceProvider -> ResourceProvider (RESTRICT - don't allow provider deletion if in use)
            entity.HasOne(wrp => wrp.ResourceProvider)
                .WithMany(rp => rp.WorldResourceProviders)
                .HasForeignKey(wrp => wrp.ResourceProviderCode)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for querying providers by world
            entity.HasIndex(wrp => wrp.WorldId);

            // Index for querying worlds by provider
            entity.HasIndex(wrp => wrp.ResourceProviderCode);
        });
    }

    private static void ConfigureQuest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quest>(entity =>
        {
            entity.HasKey(q => q.Id);

            // Title is required, max 300 characters
            entity.Property(q => q.Title)
                .HasMaxLength(300)
                .IsRequired();

            // Description is HTML from TipTap (nullable)
            entity.Property(q => q.Description);

            // Status enum
            entity.Property(q => q.Status)
                .IsRequired();

            // IsGmOnly flag
            entity.Property(q => q.IsGmOnly)
                .HasDefaultValue(false);

            // SortOrder for display ordering
            entity.Property(q => q.SortOrder)
                .HasDefaultValue(0);

            // Timestamps
            entity.Property(q => q.CreatedAt)
                .IsRequired();

            entity.Property(q => q.UpdatedAt)
                .IsRequired();

            // RowVersion for optimistic concurrency
            entity.Property(q => q.RowVersion)
                .IsRowVersion();

            // Quest -> Arc (CASCADE delete - when arc is deleted, remove its quests)
            entity.HasOne(q => q.Arc)
                .WithMany()
                .HasForeignKey(q => q.ArcId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quest -> Creator (User) (RESTRICT - don't allow user deletion if they created quests)
            entity.HasOne(q => q.Creator)
                .WithMany(u => u.CreatedQuests)
                .HasForeignKey(q => q.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes per architecture spec
            entity.HasIndex(q => q.ArcId)
                .HasDatabaseName("IX_Quest_ArcId");

            entity.HasIndex(q => new { q.ArcId, q.Status })
                .HasDatabaseName("IX_Quest_ArcId_Status");

            entity.HasIndex(q => new { q.ArcId, q.UpdatedAt })
                .HasDatabaseName("IX_Quest_ArcId_UpdatedAt");
        });
    }

    private static void ConfigureQuestUpdate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestUpdate>(entity =>
        {
            entity.HasKey(qu => qu.Id);

            // Body is HTML from TipTap (required, non-empty)
            entity.Property(qu => qu.Body)
                .IsRequired();

            // Timestamp
            entity.Property(qu => qu.CreatedAt)
                .IsRequired();

            // QuestUpdate -> Quest (CASCADE delete - when quest is deleted, remove its updates)
            entity.HasOne(qu => qu.Quest)
                .WithMany(q => q.Updates)
                .HasForeignKey(qu => qu.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuestUpdate -> Session entity (NO ACTION)
            // SQL Server disallows SET NULL here due to multiple cascade paths through
            // Quest → Arc → Campaign → World. SessionId must be nulled manually
            // in application code if a Session is ever deleted.
            entity.HasOne(qu => qu.Session)
                .WithMany(s => s.QuestUpdates)
                .HasForeignKey(qu => qu.SessionId)
                .OnDelete(DeleteBehavior.NoAction);

            // QuestUpdate -> Creator (User) (RESTRICT - don't allow user deletion if they created updates)
            entity.HasOne(qu => qu.Creator)
                .WithMany(u => u.CreatedQuestUpdates)
                .HasForeignKey(qu => qu.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes per architecture spec
            entity.HasIndex(qu => new { qu.QuestId, qu.CreatedAt })
                .HasDatabaseName("IX_QuestUpdate_QuestId_CreatedAt");

            entity.HasIndex(qu => qu.SessionId)
                .HasDatabaseName("IX_QuestUpdate_SessionId")
                .HasFilter("[SessionId] IS NOT NULL");
        });
    }

    private static void ConfigureSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .HasMaxLength(500)
                .IsRequired();

            // PublicNotes and PrivateNotes are HTML (unbounded nvarchar(max))
            entity.Property(s => s.PublicNotes);
            entity.Property(s => s.PrivateNotes);
            entity.Property(s => s.AiSummary);

            // Timestamps
            entity.Property(s => s.CreatedAt).IsRequired();
            entity.Property(s => s.ModifiedAt);  // nullable per convention

            // Session -> Arc (CASCADE delete - removing arc removes sessions)
            entity.HasOne(s => s.Arc)
                .WithMany(a => a.SessionEntities)
                .HasForeignKey(s => s.ArcId)
                .OnDelete(DeleteBehavior.Cascade);

            // Session -> Creator (User) (RESTRICT)
            entity.HasOne(s => s.Creator)
                .WithMany(u => u.CreatedSessions)
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Session -> AiSummaryGeneratedBy (User) (optional, SET NULL when user deleted not applicable
            // — use NoAction to avoid multiple cascade paths; user deletion is Restricted anyway)
            entity.HasOne(s => s.AiSummaryGeneratedBy)
                .WithMany()
                .HasForeignKey(s => s.AiSummaryGeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(s => s.ArcId)
                .HasDatabaseName("IX_Sessions_ArcId");

            entity.HasIndex(s => s.CreatedBy)
                .HasDatabaseName("IX_Sessions_CreatedBy");

            entity.HasIndex(s => new { s.ArcId, s.SessionDate })
                .HasDatabaseName("IX_Sessions_ArcId_SessionDate");
        });
    }
}
