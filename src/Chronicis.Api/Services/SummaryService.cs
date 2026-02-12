using Azure;
using Azure.AI.OpenAI;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

namespace Chronicis.Api.Services;

/// <summary>
/// Azure OpenAI implementation of AI summary generation service.
/// Supports articles, campaigns, and arcs with customizable templates.
/// </summary>
public class SummaryService : ISummaryService
{
    private readonly ChronicisDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SummaryService> _logger;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly ChatClient _chatClient;

    private const decimal InputTokenCostPer1K = 0.00040m;
    private const decimal OutputTokenCostPer1K = 0.00176m;
    private const int CharsPerToken = 4;

    // Well-known template IDs (from seed data)
    private static readonly Guid DefaultTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid CampaignRecapTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000006");

    public SummaryService(
        ChronicisDbContext context,
        IConfiguration configuration,
        ILogger<SummaryService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _configuration["AzureOpenAI:ApiKey"];
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"];

        if (string.IsNullOrEmpty(endpoint))
            throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("AzureOpenAI:ApiKey not configured");
        if (string.IsNullOrEmpty(deploymentName))
            throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured");

        _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chatClient = _openAIClient.GetChatClient(deploymentName);
    }

    #region Templates

    public async Task<List<SummaryTemplateDto>> GetTemplatesAsync()
    {
        return await _context.SummaryTemplates
            .AsNoTracking()
            .Where(t => t.IsSystem) // For now, only system templates
            .OrderBy(t => t.Name)
            .Select(t => new SummaryTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                IsSystem = t.IsSystem
            })
            .ToListAsync();
    }

    #endregion

    #region Article Summary

    public async Task<SummaryEstimateDto> EstimateArticleSummaryAsync(Guid articleId)
    {
        var article = await _context.Articles
            .AsNoTracking()
            .Include(a => a.SummaryTemplate)
            .FirstOrDefaultAsync(a => a.Id == articleId)
            ?? throw new InvalidOperationException($"Article {articleId} not found");

        var (primary, backlinks) = await GetArticleSourcesAsync(articleId);
        var promptTemplate = await GetEffectivePromptAsync(
            article.SummaryTemplateId,
            article.SummaryCustomPrompt,
            DefaultTemplateId);

        var sourceContent = FormatArticleSources(primary, backlinks);
        var fullPrompt = BuildPrompt(promptTemplate, article.Title, sourceContent, "");

        int estimatedInputTokens = fullPrompt.Length / CharsPerToken;
        int estimatedOutputTokens = int.Parse(_configuration["AzureOpenAI:MaxOutputTokens"] ?? "1500");

        decimal estimatedCost =
            (estimatedInputTokens / 1000m * InputTokenCostPer1K) +
            (estimatedOutputTokens / 1000m * OutputTokenCostPer1K);

        // Count sources: 1 for primary (if exists) + backlink count
        var sourceCount = (primary != null ? 1 : 0) + backlinks.Count;

        return new SummaryEstimateDto
        {
            EntityId = articleId,
            EntityType = "Article",
            EntityName = article.Title,
            SourceCount = sourceCount,
            EstimatedInputTokens = estimatedInputTokens,
            EstimatedOutputTokens = estimatedOutputTokens,
            EstimatedCostUSD = Math.Round(estimatedCost, 4),
            HasExistingSummary = !string.IsNullOrEmpty(article.AISummary),
            ExistingSummaryDate = article.AISummaryGeneratedAt,
            TemplateId = article.SummaryTemplateId,
            TemplateName = article.SummaryTemplate?.Name,
            CustomPrompt = article.SummaryCustomPrompt,
            IncludeWebSources = article.SummaryIncludeWebSources
        };
    }

    public async Task<SummaryGenerationDto> GenerateArticleSummaryAsync(Guid articleId, GenerateSummaryRequestDto? request = null)
    {
        try
        {
            var article = await _context.Articles
                .Include(a => a.SummaryTemplate)
                .FirstOrDefaultAsync(a => a.Id == articleId);

            if (article == null)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = $"Article {articleId} not found"
                };
            }

            var (primary, backlinks) = await GetArticleSourcesAsync(articleId);

            // Need at least the article's own content OR backlinks
            if (primary == null && backlinks.Count == 0)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = "No content available. Add content to this article or create links from other articles."
                };
            }

            // Determine effective configuration
            var templateId = request?.TemplateId ?? article.SummaryTemplateId;
            var customPrompt = request?.CustomPrompt ?? article.SummaryCustomPrompt;
            var includeWeb = request?.IncludeWebSources ?? article.SummaryIncludeWebSources;

            // Save configuration if requested
            if (request?.SaveConfiguration == true)
            {
                article.SummaryTemplateId = request.TemplateId;
                article.SummaryCustomPrompt = request.CustomPrompt;
                article.SummaryIncludeWebSources = request.IncludeWebSources;
            }

            var promptTemplate = await GetEffectivePromptAsync(templateId, customPrompt, DefaultTemplateId);
            var sourceContent = FormatArticleSources(primary, backlinks);

            // Build sources list for response
            var allSources = new List<SourceContent>();
            if (primary != null)
                allSources.Add(primary);
            allSources.AddRange(backlinks);

            // TODO: Implement web search when includeWeb is true
            var webContent = "";

            var result = await GenerateSummaryInternalAsync(
                article.Title,
                promptTemplate,
                sourceContent,
                webContent,
                allSources,
                request?.MaxOutputTokens ?? 1500);

            if (result.Success)
            {
                article.AISummary = result.Summary;
                article.AISummaryGeneratedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                result.GeneratedDate = article.AISummaryGeneratedAt.Value;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI summary for article {ArticleId}", articleId);
            return new SummaryGenerationDto
            {
                Success = false,
                ErrorMessage = $"Error generating summary: {ex.Message}"
            };
        }
    }

    public async Task<ArticleSummaryDto?> GetArticleSummaryAsync(Guid articleId)
    {
        var article = await _context.Articles
            .AsNoTracking()
            .Include(a => a.SummaryTemplate)
            .FirstOrDefaultAsync(a => a.Id == articleId);

        if (article == null)
            return null;

        return new ArticleSummaryDto
        {
            ArticleId = articleId,
            Summary = article.AISummary,
            GeneratedAt = article.AISummaryGeneratedAt,
            TemplateId = article.SummaryTemplateId,
            TemplateName = article.SummaryTemplate?.Name,
            CustomPrompt = article.SummaryCustomPrompt,
            IncludeWebSources = article.SummaryIncludeWebSources
        };
    }

    public async Task<SummaryPreviewDto?> GetArticleSummaryPreviewAsync(Guid articleId)
    {
        var article = await _context.Articles
            .AsNoTracking()
            .Include(a => a.SummaryTemplate)
            .Where(a => a.Id == articleId)
            .Select(a => new SummaryPreviewDto
            {
                Title = a.Title,
                Summary = a.AISummary,
                TemplateName = a.SummaryTemplate != null ? a.SummaryTemplate.Name : null
            })
            .FirstOrDefaultAsync();

        return article;
    }

    public async Task<bool> ClearArticleSummaryAsync(Guid articleId)
    {
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == articleId);
        if (article == null)
            return false;

        article.AISummary = null;
        article.AISummaryGeneratedAt = null;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<(SourceContent? Primary, List<SourceContent> Backlinks)> GetArticleSourcesAsync(Guid articleId)
    {
        // Get the article's own content as the primary/canonical source
        var article = await _context.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId);

        SourceContent? primarySource = null;
        if (article != null && !string.IsNullOrEmpty(article.Body))
        {
            primarySource = new SourceContent
            {
                Type = "Primary",
                Title = article.Title,
                Content = article.Body,
                ArticleId = article.Id
            };
        }

        // Get all articles that link TO this article (backlinks)
        var backlinks = await _context.ArticleLinks
            .AsNoTracking()
            .Where(al => al.TargetArticleId == articleId)
            .Select(al => al.SourceArticle)
            .Distinct()
            .Where(a => !string.IsNullOrEmpty(a.Body) && a.Visibility == ArticleVisibility.Public)
            .Select(a => new SourceContent
            {
                Type = "Backlink",
                Title = a.Title,
                Content = a.Body!,
                ArticleId = a.Id
            })
            .ToListAsync();

        return (primarySource, backlinks);
    }

    #endregion

    #region Campaign Summary

    public async Task<SummaryEstimateDto> EstimateCampaignSummaryAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .AsNoTracking()
            .Include(c => c.SummaryTemplate)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");

        var sources = await GetCampaignSourcesAsync(campaignId);
        var promptTemplate = await GetEffectivePromptAsync(
            campaign.SummaryTemplateId,
            campaign.SummaryCustomPrompt,
            CampaignRecapTemplateId);

        var sourceContent = FormatSources(sources);
        var fullPrompt = BuildPrompt(promptTemplate, campaign.Name, sourceContent, "");

        int estimatedInputTokens = fullPrompt.Length / CharsPerToken;
        int estimatedOutputTokens = int.Parse(_configuration["AzureOpenAI:MaxOutputTokens"] ?? "1500");

        decimal estimatedCost =
            (estimatedInputTokens / 1000m * InputTokenCostPer1K) +
            (estimatedOutputTokens / 1000m * OutputTokenCostPer1K);

        return new SummaryEstimateDto
        {
            EntityId = campaignId,
            EntityType = "Campaign",
            EntityName = campaign.Name,
            SourceCount = sources.Count,
            EstimatedInputTokens = estimatedInputTokens,
            EstimatedOutputTokens = estimatedOutputTokens,
            EstimatedCostUSD = Math.Round(estimatedCost, 4),
            HasExistingSummary = !string.IsNullOrEmpty(campaign.AISummary),
            ExistingSummaryDate = campaign.AISummaryGeneratedAt,
            TemplateId = campaign.SummaryTemplateId,
            TemplateName = campaign.SummaryTemplate?.Name,
            CustomPrompt = campaign.SummaryCustomPrompt,
            IncludeWebSources = campaign.SummaryIncludeWebSources
        };
    }

    public async Task<SummaryGenerationDto> GenerateCampaignSummaryAsync(Guid campaignId, GenerateSummaryRequestDto? request = null)
    {
        try
        {
            var campaign = await _context.Campaigns
                .Include(c => c.SummaryTemplate)
                .FirstOrDefaultAsync(c => c.Id == campaignId);

            if (campaign == null)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = $"Campaign {campaignId} not found"
                };
            }

            var sources = await GetCampaignSourcesAsync(campaignId);

            if (sources.Count == 0)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = "No public session notes found in this campaign."
                };
            }

            var templateId = request?.TemplateId ?? campaign.SummaryTemplateId;
            var customPrompt = request?.CustomPrompt ?? campaign.SummaryCustomPrompt;
            var includeWeb = request?.IncludeWebSources ?? campaign.SummaryIncludeWebSources;

            if (request?.SaveConfiguration == true)
            {
                campaign.SummaryTemplateId = request.TemplateId;
                campaign.SummaryCustomPrompt = request.CustomPrompt;
                campaign.SummaryIncludeWebSources = request.IncludeWebSources;
            }

            var promptTemplate = await GetEffectivePromptAsync(templateId, customPrompt, CampaignRecapTemplateId);
            var sourceContent = FormatSources(sources);
            var webContent = "";

            var result = await GenerateSummaryInternalAsync(
                campaign.Name,
                promptTemplate,
                sourceContent,
                webContent,
                sources,
                request?.MaxOutputTokens ?? 1500);

            if (result.Success)
            {
                campaign.AISummary = result.Summary;
                campaign.AISummaryGeneratedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                result.GeneratedDate = campaign.AISummaryGeneratedAt.Value;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI summary for campaign {CampaignId}", campaignId);
            return new SummaryGenerationDto
            {
                Success = false,
                ErrorMessage = $"Error generating summary: {ex.Message}"
            };
        }
    }

    public async Task<EntitySummaryDto?> GetCampaignSummaryAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .AsNoTracking()
            .Include(c => c.SummaryTemplate)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return null;

        return new EntitySummaryDto
        {
            EntityId = campaignId,
            EntityType = "Campaign",
            Summary = campaign.AISummary,
            GeneratedAt = campaign.AISummaryGeneratedAt,
            TemplateId = campaign.SummaryTemplateId,
            TemplateName = campaign.SummaryTemplate?.Name,
            CustomPrompt = campaign.SummaryCustomPrompt,
            IncludeWebSources = campaign.SummaryIncludeWebSources
        };
    }

    public async Task<bool> ClearCampaignSummaryAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
        if (campaign == null)
            return false;

        campaign.AISummary = null;
        campaign.AISummaryGeneratedAt = null;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<List<SourceContent>> GetCampaignSourcesAsync(Guid campaignId)
    {
        // Get all public session articles in this campaign
        var sessions = await _context.Articles
            .AsNoTracking()
            .Where(a => a.CampaignId == campaignId
                && a.Type == ArticleType.Session
                && a.Visibility == ArticleVisibility.Public
                && !string.IsNullOrEmpty(a.Body))
            .OrderBy(a => a.SessionDate ?? a.CreatedAt)
            .Select(a => new SourceContent
            {
                Type = "Session",
                Title = a.Title,
                Content = a.Body!,
                ArticleId = a.Id
            })
            .ToListAsync();

        return sessions;
    }

    #endregion

    #region Arc Summary

    public async Task<SummaryEstimateDto> EstimateArcSummaryAsync(Guid arcId)
    {
        var arc = await _context.Arcs
            .AsNoTracking()
            .Include(a => a.SummaryTemplate)
            .FirstOrDefaultAsync(a => a.Id == arcId)
            ?? throw new InvalidOperationException($"Arc {arcId} not found");

        var sources = await GetArcSourcesAsync(arcId);
        var promptTemplate = await GetEffectivePromptAsync(
            arc.SummaryTemplateId,
            arc.SummaryCustomPrompt,
            CampaignRecapTemplateId);

        var sourceContent = FormatSources(sources);
        var fullPrompt = BuildPrompt(promptTemplate, arc.Name, sourceContent, "");

        int estimatedInputTokens = fullPrompt.Length / CharsPerToken;
        int estimatedOutputTokens = int.Parse(_configuration["AzureOpenAI:MaxOutputTokens"] ?? "1500");

        decimal estimatedCost =
            (estimatedInputTokens / 1000m * InputTokenCostPer1K) +
            (estimatedOutputTokens / 1000m * OutputTokenCostPer1K);

        return new SummaryEstimateDto
        {
            EntityId = arcId,
            EntityType = "Arc",
            EntityName = arc.Name,
            SourceCount = sources.Count,
            EstimatedInputTokens = estimatedInputTokens,
            EstimatedOutputTokens = estimatedOutputTokens,
            EstimatedCostUSD = Math.Round(estimatedCost, 4),
            HasExistingSummary = !string.IsNullOrEmpty(arc.AISummary),
            ExistingSummaryDate = arc.AISummaryGeneratedAt,
            TemplateId = arc.SummaryTemplateId,
            TemplateName = arc.SummaryTemplate?.Name,
            CustomPrompt = arc.SummaryCustomPrompt,
            IncludeWebSources = arc.SummaryIncludeWebSources
        };
    }

    public async Task<SummaryGenerationDto> GenerateArcSummaryAsync(Guid arcId, GenerateSummaryRequestDto? request = null)
    {
        try
        {
            var arc = await _context.Arcs
                .Include(a => a.SummaryTemplate)
                .FirstOrDefaultAsync(a => a.Id == arcId);

            if (arc == null)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = $"Arc {arcId} not found"
                };
            }

            var sources = await GetArcSourcesAsync(arcId);

            if (sources.Count == 0)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = "No public session notes found in this arc."
                };
            }

            var templateId = request?.TemplateId ?? arc.SummaryTemplateId;
            var customPrompt = request?.CustomPrompt ?? arc.SummaryCustomPrompt;
            var includeWeb = request?.IncludeWebSources ?? arc.SummaryIncludeWebSources;

            if (request?.SaveConfiguration == true)
            {
                arc.SummaryTemplateId = request.TemplateId;
                arc.SummaryCustomPrompt = request.CustomPrompt;
                arc.SummaryIncludeWebSources = request.IncludeWebSources;
            }

            var promptTemplate = await GetEffectivePromptAsync(templateId, customPrompt, CampaignRecapTemplateId);
            var sourceContent = FormatSources(sources);
            var webContent = "";

            var result = await GenerateSummaryInternalAsync(
                arc.Name,
                promptTemplate,
                sourceContent,
                webContent,
                sources,
                request?.MaxOutputTokens ?? 1500);

            if (result.Success)
            {
                arc.AISummary = result.Summary;
                arc.AISummaryGeneratedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                result.GeneratedDate = arc.AISummaryGeneratedAt.Value;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI summary for arc {ArcId}", arcId);
            return new SummaryGenerationDto
            {
                Success = false,
                ErrorMessage = $"Error generating summary: {ex.Message}"
            };
        }
    }

    public async Task<EntitySummaryDto?> GetArcSummaryAsync(Guid arcId)
    {
        var arc = await _context.Arcs
            .AsNoTracking()
            .Include(a => a.SummaryTemplate)
            .FirstOrDefaultAsync(a => a.Id == arcId);

        if (arc == null)
            return null;

        return new EntitySummaryDto
        {
            EntityId = arcId,
            EntityType = "Arc",
            Summary = arc.AISummary,
            GeneratedAt = arc.AISummaryGeneratedAt,
            TemplateId = arc.SummaryTemplateId,
            TemplateName = arc.SummaryTemplate?.Name,
            CustomPrompt = arc.SummaryCustomPrompt,
            IncludeWebSources = arc.SummaryIncludeWebSources
        };
    }

    public async Task<bool> ClearArcSummaryAsync(Guid arcId)
    {
        var arc = await _context.Arcs.FirstOrDefaultAsync(a => a.Id == arcId);
        if (arc == null)
            return false;

        arc.AISummary = null;
        arc.AISummaryGeneratedAt = null;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<List<SourceContent>> GetArcSourcesAsync(Guid arcId)
    {
        // Get all public session articles in this arc
        var sessions = await _context.Articles
            .AsNoTracking()
            .Where(a => a.ArcId == arcId
                && a.Type == ArticleType.Session
                && a.Visibility == ArticleVisibility.Public
                && !string.IsNullOrEmpty(a.Body))
            .OrderBy(a => a.SessionDate ?? a.CreatedAt)
            .Select(a => new SourceContent
            {
                Type = "Session",
                Title = a.Title,
                Content = a.Body!,
                ArticleId = a.Id
            })
            .ToListAsync();

        return sessions;
    }

    #endregion

    #region Internal Helpers

    private async Task<string> GetEffectivePromptAsync(Guid? templateId, string? customPrompt, Guid defaultTemplateId)
    {
        // Custom prompt is used as additional instructions, not a full replacement
        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            // Wrap custom prompt with source content structure  
            return $@"You are analyzing tabletop RPG campaign notes about: {{EntityName}}

Here are the source materials. The CANONICAL CONTENT is from the article itself and should be treated as authoritative facts. The REFERENCES show how other articles mention this entity and provide additional context.

{{SourceContent}}

{{WebContent}}

Custom instructions from the user:
{customPrompt}

Based on the source materials above and following the custom instructions, provide a comprehensive summary. Treat the canonical content as the primary source of truth.";
        }

        // Use specified template or default
        var effectiveTemplateId = templateId ?? defaultTemplateId;

        var template = await _context.SummaryTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == effectiveTemplateId);

        if (template != null)
        {
            return template.PromptTemplate;
        }

        // Fallback to default template
        template = await _context.SummaryTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == defaultTemplateId);

        return template?.PromptTemplate ?? throw new InvalidOperationException("Default template not found");
    }

    private static string BuildPrompt(string template, string entityName, string sourceContent, string webContent)
    {
        return template
            .Replace("{EntityName}", entityName)
            .Replace("{SourceContent}", sourceContent)
            .Replace("{WebContent}", string.IsNullOrEmpty(webContent) ? "" : $"\n\nAdditional context from external sources:\n{webContent}");
    }

    private static string FormatSources(List<SourceContent> sources)
    {
        return string.Join("\n\n", sources.Select(s =>
            $"--- From: {s.Title} ({s.Type}) ---\n{s.Content}\n---"));
    }

    private static string FormatArticleSources(SourceContent? primary, List<SourceContent> backlinks)
    {
        var parts = new List<string>();

        if (primary != null)
        {
            parts.Add($"=== CANONICAL CONTENT (from the article itself) ===\n{primary.Content}\n===");
        }

        if (backlinks.Any())
        {
            parts.Add("=== REFERENCES FROM OTHER ARTICLES ===");
            foreach (var backlink in backlinks)
            {
                parts.Add($"--- From: {backlink.Title} ---\n{backlink.Content}\n---");
            }
        }

        return string.Join("\n\n", parts);
    }


    private async Task<SummaryGenerationDto> GenerateSummaryInternalAsync(
        string entityName,
        string promptTemplate,
        string sourceContent,
        string webContent,
        List<SourceContent> sources,
        int maxOutputTokens)
    {
        var prompt = BuildPrompt(promptTemplate, entityName, sourceContent, webContent);

        var maxInputTokens = int.Parse(_configuration["AzureOpenAI:MaxInputTokens"] ?? "8000");
        if (prompt.Length / CharsPerToken > maxInputTokens)
        {
            _logger.LogWarning("Prompt exceeds max input tokens, truncating content");
            var maxContentLength = maxInputTokens * CharsPerToken - (promptTemplate.Length + entityName.Length + 200);
            var truncatedSourceContent = sourceContent.Substring(0, Math.Min(sourceContent.Length, maxContentLength));
            prompt = BuildPrompt(promptTemplate, entityName, truncatedSourceContent, webContent);
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful assistant that summarizes tabletop RPG campaign notes."),
            new UserChatMessage(prompt)
        };

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxOutputTokens,
            Temperature = 0.7f
        };

        var completion = await _chatClient.CompleteChatAsync(messages, chatOptions);

        var summary = completion.Value.Content[0].Text;
        var inputTokens = completion.Value.Usage.InputTokenCount;
        var outputTokens = completion.Value.Usage.OutputTokenCount;

        var actualCost =
            (inputTokens / 1000m * InputTokenCostPer1K) +
            (outputTokens / 1000m * OutputTokenCostPer1K);

        return new SummaryGenerationDto
        {
            Success = true,
            Summary = summary,
            TokensUsed = completion.Value.Usage.TotalTokenCount,
            ActualCostUSD = Math.Round(actualCost, 4),
            Sources = sources.Select(s => new SummarySourceDto
            {
                Type = s.Type,
                Title = s.Title,
                ArticleId = s.ArticleId
            }).ToList()
        };
    }

    #endregion
}

/// <summary>
/// Internal class for holding source content during processing
/// </summary>
internal class SourceContent
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ArticleId { get; set; }
}
