using Azure;
using Azure.AI.OpenAI;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Chronicis.Api.Services;

public interface IAISummaryService
{
    Task<SummaryEstimateDto> EstimateCostAsync(int articleId);
    Task<SummaryGenerationDto> GenerateSummaryAsync(int articleId, int maxOutputTokens = 1500);
}

public class AISummaryService : IAISummaryService
{
    private readonly ChronicisDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AISummaryService> _logger;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly ChatClient _chatClient;

    // Pricing constants (GPT-4 as of Nov 2024)
    private const decimal INPUT_TOKEN_COST_PER_1K = 0.00040m;   // Cost per 1K input tokens for gpt-4.1-mini
    private const decimal OUTPUT_TOKEN_COST_PER_1K = 0.00160m;  // $0.06 per 1K output tokens for gpt-4.1-mini
    private const int CHARS_PER_TOKEN = 4; // Rough estimate: 1 token ≈ 4 characters

    public AISummaryService(
        ChronicisDbContext context,
        IConfiguration configuration,
        ILogger<AISummaryService> logger)
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

    public async Task<SummaryEstimateDto> EstimateCostAsync(int articleId)
    {
        var article = await _context.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId);

        if (article == null)
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        // Get backlinks
        var backlinks = await GetBacklinksContentAsync(articleId);

        // Estimate tokens
        var promptTemplate = _configuration["AzureOpenAI:SummaryPromptTemplate"] ?? string.Empty;
        var backlinksContent = string.Join("\n\n", backlinks.Select(b =>
            $"--- From: {b.Title} ---\n{b.Content}"));

        var fullPrompt = promptTemplate
            .Replace("{ArticleTitle}", article.Title)
            .Replace("{BacklinkContent}", backlinksContent);

        int estimatedInputTokens = fullPrompt.Length / CHARS_PER_TOKEN;
        int estimatedOutputTokens = int.Parse(_configuration["AzureOpenAI:MaxOutputTokens"] ?? "1500");

        // Calculate cost
        decimal estimatedCost =
            (estimatedInputTokens / 1000m * INPUT_TOKEN_COST_PER_1K) +
            (estimatedOutputTokens / 1000m * OUTPUT_TOKEN_COST_PER_1K);

        return new SummaryEstimateDto
        {
            ArticleId = articleId,
            ArticleTitle = article.Title,
            BacklinkCount = backlinks.Count,
            EstimatedInputTokens = estimatedInputTokens,
            EstimatedOutputTokens = estimatedOutputTokens,
            EstimatedCostUSD = Math.Round(estimatedCost, 4),
            HasExistingSummary = !string.IsNullOrEmpty(article.AISummary),
            ExistingSummaryDate = article.AISummaryGeneratedDate
        };
    }

    public async Task<SummaryGenerationDto> GenerateSummaryAsync(int articleId, int maxOutputTokens = 1500)
    {
        try
        {
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == articleId);
            if (article == null)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = $"Article {articleId} not found"
                };
            }

            // Get backlinks
            var backlinks = await GetBacklinksContentAsync(articleId);

            if (backlinks.Count == 0)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = "No backlinks found. This article is not referenced by any other articles yet."
                };
            }

            // Build prompt
            var promptTemplate = _configuration["AzureOpenAI:SummaryPromptTemplate"]
                ?? throw new InvalidOperationException("SummaryPromptTemplate not configured");

            var backlinksContent = string.Join("\n\n", backlinks.Select(b =>
                $"--- From: {b.Title} ---\n{b.Content}\n---"));

            var prompt = promptTemplate
                .Replace("{ArticleTitle}", article.Title)
                .Replace("{BacklinkContent}", backlinksContent);

            // Check token limits
            var maxInputTokens = int.Parse(_configuration["AzureOpenAI:MaxInputTokens"] ?? "8000");
            if (prompt.Length / CHARS_PER_TOKEN > maxInputTokens)
            {
                _logger.LogWarning("Prompt exceeds max input tokens, truncating content");
                // Truncate backlinks content to fit
                var maxContentLength = maxInputTokens * CHARS_PER_TOKEN - (promptTemplate.Length - "{BacklinkContent}".Length);
                backlinksContent = backlinksContent.Substring(0, Math.Min(backlinksContent.Length, maxContentLength));
                prompt = promptTemplate
                    .Replace("{ArticleTitle}", article.Title)
                    .Replace("{BacklinkContent}", backlinksContent);
            }

            // Call Azure OpenAI using new 2.1.0 API
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful assistant that summarizes D&D campaign notes."),
                new UserChatMessage(prompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = maxOutputTokens,
                Temperature = 0.7f
            };

            _logger.LogInformation("Generating AI summary for article {ArticleId} with {BacklinkCount} backlinks",
                articleId, backlinks.Count);

            var completion = await _chatClient.CompleteChatAsync(messages, chatOptions);

            var summary = completion.Value.Content[0].Text;
            var tokensUsed = completion.Value.Usage.TotalTokenCount;
            var inputTokens = completion.Value.Usage.InputTokenCount;
            var outputTokens = completion.Value.Usage.OutputTokenCount;

            // Calculate actual cost
            var actualCost =
                (inputTokens / 1000m * INPUT_TOKEN_COST_PER_1K) +
                (outputTokens / 1000m * OUTPUT_TOKEN_COST_PER_1K);

            // Save summary to database
            article.AISummary = summary;
            article.AISummaryGeneratedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log to Application Insights
            _logger.LogInformation(
                "AI Summary generated for article {ArticleId}. Tokens: {Tokens}, Cost: ${Cost:F4}",
                articleId, tokensUsed, actualCost);

            return new SummaryGenerationDto
            {
                Success = true,
                Summary = summary,
                GeneratedDate = article.AISummaryGeneratedDate.Value,
                TokensUsed = tokensUsed,
                ActualCostUSD = Math.Round(actualCost, 4)
            };
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

    private async Task<List<(string Title, string Content)>> GetBacklinksContentAsync(int articleId)
    {
        // Find hashtags that link to this article
        var linkedHashtags = await _context.Hashtags
            .Where(h => h.LinkedArticleId == articleId)
            .Select(h => h.Id)
            .ToListAsync();

        if (linkedHashtags.Count == 0)
        {
            return new List<(string, string)>();
        }

        // Find articles that use those hashtags (excluding the source article)
        var backlinks = await _context.ArticleHashtags
            .Include(ah => ah.Article)
            .Where(ah => linkedHashtags.Contains(ah.HashtagId) && ah.ArticleId != articleId)
            .Select(ah => new { ah.Article.Id, ah.Article.Title, ah.Article.Body })
            .Distinct()
            .ToListAsync();

        return backlinks.Select(b => (b.Title, b.Body)).ToList();
    }
}