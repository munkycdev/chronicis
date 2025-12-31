using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for generating contextual prompts based on user state.
/// </summary>
public class PromptService : IPromptService
{
    private readonly ILogger<PromptService> _logger;
    
    // Configuration constants
    private const int StaleCharacterDays = 14;
    private const int SessionFollowUpDays = 7;
    private const int MaxPrompts = 3;

    public PromptService(ILogger<PromptService> logger)
    {
        _logger = logger;
    }

    public List<PromptDto> GeneratePrompts(DashboardDto dashboard)
    {
        var prompts = new List<PromptDto>();

        // Priority 1: Missing fundamentals
        AddMissingFundamentalPrompts(dashboard, prompts);

        // Priority 2: Stale content needing attention
        AddStaleContentPrompts(dashboard, prompts);

        // Priority 3: Session follow-up
        AddSessionFollowUpPrompts(dashboard, prompts);

        // Priority 4: Engagement suggestions
        AddEngagementPrompts(dashboard, prompts);

        // Priority 5: General tips (fallback)
        AddGeneralTips(dashboard, prompts);

        // Return top N prompts by priority
        return prompts
            .OrderBy(p => p.Priority)
            .Take(MaxPrompts)
            .ToList();
    }

    private void AddMissingFundamentalPrompts(DashboardDto dashboard, List<PromptDto> prompts)
    {
        // No worlds at all
        if (!dashboard.Worlds.Any())
        {
            prompts.Add(new PromptDto
            {
                Key = "no-worlds",
                Title = "Create Your First World",
                Message = "Every great campaign starts with a world. Create one to begin organizing your adventures.",
                Icon = "🌍",
                ActionText = "Create World",
                ActionUrl = "/dashboard", // Dashboard has the create button
                Priority = 10,
                Category = PromptCategory.MissingFundamental
            });
            return; // Don't add more prompts if no worlds
        }

        // Has world but no campaigns
        var worldsWithoutCampaigns = dashboard.Worlds.Where(w => !w.Campaigns.Any()).ToList();
        if (worldsWithoutCampaigns.Any())
        {
            var world = worldsWithoutCampaigns.First();
            prompts.Add(new PromptDto
            {
                Key = $"no-campaign-{world.Id}",
                Title = "Start a Campaign",
                Message = $"Your world \"{world.Name}\" doesn't have any campaigns yet. Create one to track your adventures!",
                Icon = "📜",
                ActionText = "View World",
                ActionUrl = $"/world/{world.Slug}",
                Priority = 20,
                Category = PromptCategory.MissingFundamental
            });
        }

        // Has campaigns but no claimed characters
        if (!dashboard.ClaimedCharacters.Any() && dashboard.Worlds.Any(w => w.Campaigns.Any()))
        {
            prompts.Add(new PromptDto
            {
                Key = "no-characters",
                Title = "Claim Your Character",
                Message = "You haven't claimed any characters yet. Find your character in the tree and click 'Claim as My Character' to see them on your dashboard.",
                Icon = "👤",
                Priority = 30,
                Category = PromptCategory.MissingFundamental
            });
        }

        // Has campaign but no sessions
        var campaignsWithoutSessions = dashboard.Worlds
            .SelectMany(w => w.Campaigns)
            .Where(c => c.SessionCount == 0)
            .ToList();
        
        if (campaignsWithoutSessions.Any())
        {
            var campaign = campaignsWithoutSessions.First();
            var world = dashboard.Worlds.First(w => w.Campaigns.Contains(campaign));
            prompts.Add(new PromptDto
            {
                Key = $"no-sessions-{campaign.Id}",
                Title = "Record Your First Session",
                Message = $"Campaign \"{campaign.Name}\" has no session notes yet. Start documenting your adventures!",
                Icon = "📝",
                ActionText = "Add Session",
                ActionUrl = $"/world/{world.Slug}",
                Priority = 40,
                Category = PromptCategory.MissingFundamental
            });
        }
    }

    private void AddStaleContentPrompts(DashboardDto dashboard, List<PromptDto> prompts)
    {
        var staleThreshold = DateTime.UtcNow.AddDays(-StaleCharacterDays);

        // Stale characters (not updated in 14+ days)
        var staleCharacters = dashboard.ClaimedCharacters
            .Where(c => (c.ModifiedAt ?? c.CreatedAt) < staleThreshold)
            .OrderBy(c => c.ModifiedAt ?? c.CreatedAt)
            .ToList();

        if (staleCharacters.Any())
        {
            var stalest = staleCharacters.First();
            var daysSinceUpdate = (int)(DateTime.UtcNow - (stalest.ModifiedAt ?? stalest.CreatedAt)).TotalDays;
            
            prompts.Add(new PromptDto
            {
                Key = $"stale-character-{stalest.Id}",
                Title = "Update Your Character",
                Message = $"\"{stalest.Title}\" hasn't been updated in {daysSinceUpdate} days. How have they grown since your last session?",
                Icon = "✨",
                ActionText = "View Character",
                ActionUrl = $"/article/{stalest.Id}",
                Priority = 50,
                Category = PromptCategory.NeedsAttention
            });
        }
    }

    private void AddSessionFollowUpPrompts(DashboardDto dashboard, List<PromptDto> prompts)
    {
        var followUpThreshold = DateTime.UtcNow.AddDays(-SessionFollowUpDays);

        // Find campaigns with recent sessions that might need follow-up notes
        foreach (var world in dashboard.Worlds)
        {
            foreach (var campaign in world.Campaigns.Where(c => c.IsActive))
            {
                if (campaign.CurrentArc?.LatestSessionDate != null)
                {
                    var lastSession = campaign.CurrentArc.LatestSessionDate.Value;
                    var daysSinceSession = (int)(DateTime.UtcNow - lastSession).TotalDays;

                    // Session was 2-7 days ago - good time to add notes while fresh
                    if (daysSinceSession >= 2 && daysSinceSession <= SessionFollowUpDays)
                    {
                        prompts.Add(new PromptDto
                        {
                            Key = $"session-followup-{campaign.Id}",
                            Title = "Add Session Notes",
                            Message = $"Your last session in \"{campaign.Name}\" was {daysSinceSession} days ago. Add notes while it's still fresh!",
                            Icon = "📖",
                            ActionText = "View Campaign",
                            ActionUrl = $"/world/{world.Slug}",
                            Priority = 60,
                            Category = PromptCategory.NeedsAttention
                        });
                        break; // Only one session follow-up prompt
                    }
                }
            }
        }
    }

    private void AddEngagementPrompts(DashboardDto dashboard, List<PromptDto> prompts)
    {
        // Only add engagement prompts if user has some content
        if (!dashboard.Worlds.Any())
            return;

        var totalArticles = dashboard.Worlds.Sum(w => w.ArticleCount);

        // Suggest wiki links if they have multiple articles
        if (totalArticles >= 5)
        {
            prompts.Add(new PromptDto
            {
                Key = "try-wiki-links",
                Title = "Connect Your Content",
                Message = "Use [[Article Name]] in your notes to create links between articles. It's a great way to build a connected knowledge base!",
                Icon = "🔗",
                Priority = 70,
                Category = PromptCategory.Suggestion
            });
        }

        // Suggest AI summaries if they have backlinks
        if (totalArticles >= 10)
        {
            prompts.Add(new PromptDto
            {
                Key = "try-ai-summaries",
                Title = "Try AI Summaries",
                Message = "Articles with backlinks can generate AI summaries. Open an article and click 'Generate Summary' to try it out!",
                Icon = "🤖",
                Priority = 75,
                Category = PromptCategory.Suggestion
            });
        }
    }

    private void AddGeneralTips(DashboardDto dashboard, List<PromptDto> prompts)
    {
        // Only add tips if we don't have enough higher-priority prompts
        if (prompts.Count >= MaxPrompts)
            return;

        var tips = new List<PromptDto>
        {
            new()
            {
                Key = "tip-keyboard-shortcuts",
                Title = "Keyboard Shortcuts",
                Message = "Press Ctrl+S to save, Ctrl+N to create a new article. Your work auto-saves as you type!",
                Icon = "⌨️",
                Priority = 100,
                Category = PromptCategory.Tip
            },
            new()
            {
                Key = "tip-tree-search",
                Title = "Quick Navigation",
                Message = "Use the search box in the sidebar to quickly filter articles by title.",
                Icon = "🔍",
                Priority = 101,
                Category = PromptCategory.Tip
            },
            new()
            {
                Key = "tip-drag-drop",
                Title = "Organize with Drag & Drop",
                Message = "Drag articles in the tree to reorganize your content hierarchy.",
                Icon = "📂",
                Priority = 102,
                Category = PromptCategory.Tip
            },
            new()
            {
                Key = "tip-backlinks",
                Title = "Discover Connections",
                Message = "Click the metadata button on any article to see what other articles link to it.",
                Icon = "🔙",
                Priority = 103,
                Category = PromptCategory.Tip
            }
        };

        // Add a random tip if we need more prompts
        var random = new Random();
        var shuffledTips = tips.OrderBy(_ => random.Next()).ToList();
        
        foreach (var tip in shuffledTips)
        {
            if (prompts.Count < MaxPrompts)
            {
                prompts.Add(tip);
            }
        }
    }
}
