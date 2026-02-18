using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class PromptServiceTests
{
    private readonly PromptService _sut;

    public PromptServiceTests()
    {
        _sut = new PromptService(NullLogger<PromptService>.Instance);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static DashboardDto EmptyDashboard() => new();

    private static DashboardDto DashboardWithWorld(
        bool withCampaign = false,
        bool withSessions = false,
        bool campaignActive = false,
        DateTime? latestSessionDate = null)
    {
        var dashboard = new DashboardDto
        {
            Worlds = new List<DashboardWorldDto>
            {
                new DashboardWorldDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Test World",
                    Slug = "test-world",
                    ArticleCount = 3,
                    Campaigns = withCampaign
                        ? new List<DashboardCampaignDto>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(),
                                Name = "Test Campaign",
                                IsActive = campaignActive,
                                SessionCount = withSessions ? 5 : 0,
                                CurrentArc = latestSessionDate.HasValue
                                    ? new DashboardArcDto { LatestSessionDate = latestSessionDate }
                                    : null
                            }
                        }
                        : new List<DashboardCampaignDto>()
                }
            }
        };
        return dashboard;
    }

    // ── No worlds ────────────────────────────────────────────────

    [Fact]
    public void GeneratePrompts_NoWorlds_SuggestsCreateWorld()
    {
        var result = _sut.GeneratePrompts(EmptyDashboard());

        Assert.Contains(result, p => p.Key == "no-worlds");
    }

    [Fact]
    public void GeneratePrompts_WorldWithoutCampaign_SuggestsStartCampaign()
    {
        var dashboard = DashboardWithWorld(withCampaign: false);

        var result = _sut.GeneratePrompts(dashboard);

        Assert.Contains(result, p => p.Key.StartsWith("no-campaign-"));
    }

    [Fact]
    public void GeneratePrompts_CampaignWithoutSessions_SuggestsRecordSession()
    {
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: false);

        var result = _sut.GeneratePrompts(dashboard);

        Assert.Contains(result, p => p.Key.StartsWith("no-sessions-"));
    }

    [Fact]
    public void GeneratePrompts_NoClaimed_SuggestsClaimCharacter()
    {
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: true);
        // No claimed characters

        var result = _sut.GeneratePrompts(dashboard);

        Assert.Contains(result, p => p.Key == "no-characters");
    }

    [Fact]
    public void GeneratePrompts_StaleCharacter_SuggestsUpdate()
    {
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: true);
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Gandalf",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                ModifiedAt = DateTime.UtcNow.AddDays(-20)
            }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.Contains(result, p => p.Key.StartsWith("stale-character-"));
    }

    [Fact]
    public void GeneratePrompts_StaleCharacterWithNullModifiedAt_UsesCreatedAt()
    {
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: true);
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Bilbo",
                CreatedAt = DateTime.UtcNow.AddDays(-21),
                ModifiedAt = null
            }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.Contains(result, p => p.Key.StartsWith("stale-character-"));
    }

    [Fact]
    public void GeneratePrompts_RecentSession_SuggestsFollowUp()
    {
        var dashboard = DashboardWithWorld(
            withCampaign: true,
            withSessions: true,
            campaignActive: true,
            latestSessionDate: DateTime.UtcNow.AddDays(-3));
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Fresh Char", CreatedAt = DateTime.UtcNow }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.Contains(result, p => p.Key.StartsWith("session-followup-"));
    }

    [Fact]
    public void GeneratePrompts_SessionOutsideFollowUpWindow_DoesNotSuggestFollowUp()
    {
        var dashboard = DashboardWithWorld(
            withCampaign: true,
            withSessions: true,
            campaignActive: true,
            latestSessionDate: DateTime.UtcNow.AddDays(-10));
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Fresh Char", CreatedAt = DateTime.UtcNow }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.DoesNotContain(result, p => p.Key.StartsWith("session-followup-"));
    }

    [Fact]
    public void GeneratePrompts_OneDaySinceSession_DoesNotSuggestFollowUp()
    {
        var dashboard = DashboardWithWorld(
            withCampaign: true,
            withSessions: true,
            campaignActive: true,
            latestSessionDate: DateTime.UtcNow.AddDays(-1));
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Fresh Char", CreatedAt = DateTime.UtcNow }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.DoesNotContain(result, p => p.Key.StartsWith("session-followup-"));
    }

    [Fact]
    public void GeneratePrompts_CurrentArcWithoutSessionDate_DoesNotSuggestFollowUp()
    {
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: true, campaignActive: true);
        dashboard.Worlds[0].Campaigns[0].CurrentArc = new DashboardArcDto();
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Fresh Char", CreatedAt = DateTime.UtcNow }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.DoesNotContain(result, p => p.Key.StartsWith("session-followup-"));
    }

    [Fact]
    public void GeneratePrompts_ActiveCampaignWithoutCurrentArc_DoesNotSuggestFollowUp()
    {
        var dashboard = DashboardWithWorld(
            withCampaign: true,
            withSessions: true,
            campaignActive: true,
            latestSessionDate: null);
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Fresh Char", CreatedAt = DateTime.UtcNow }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.DoesNotContain(result, p => p.Key.StartsWith("session-followup-"));
    }

    [Fact]
    public void GeneratePrompts_ManyArticles_SuggestsWikiLinks()
    {
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: true);
        dashboard.Worlds[0].ArticleCount = 5;
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Char", CreatedAt = DateTime.UtcNow }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.Contains(result, p => p.Key == "try-wiki-links");
    }

    [Fact]
    public void GeneratePrompts_TenArticles_SuggestsAiSummaries()
    {
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: true);
        dashboard.Worlds[0].ArticleCount = 10;
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Char", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        var result = _sut.GeneratePrompts(dashboard);

        Assert.Contains(result, p => p.Key == "try-ai-summaries");
    }

    [Fact]
    public void GeneratePrompts_ReturnsMaxThreePrompts()
    {
        // Dashboard with everything missing to trigger many prompts
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: false);

        var result = _sut.GeneratePrompts(dashboard);

        Assert.True(result.Count <= 3);
    }

    [Fact]
    public void GeneratePrompts_OrdersByPriority()
    {
        var dashboard = DashboardWithWorld(withCampaign: false);

        var result = _sut.GeneratePrompts(dashboard);

        var priorities = result.Select(p => p.Priority).ToList();
        Assert.Equal(priorities.OrderBy(p => p).ToList(), priorities);
    }

    [Fact]
    public void GeneratePrompts_FillsWithTips_WhenFewHighPriorityPrompts()
    {
        // World with campaign and sessions, claimed character that's fresh → few prompts
        var dashboard = DashboardWithWorld(withCampaign: true, withSessions: true);
        dashboard.ClaimedCharacters = new List<ClaimedCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Char", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow }
        };

        var result = _sut.GeneratePrompts(dashboard);

        // Should have 3 results, padded with tips
        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Category == PromptCategory.Tip);
    }
}
