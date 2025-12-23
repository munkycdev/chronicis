using Azure;
using Azure.AI.OpenAI;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Chronicis.Api.Services;

/// <summary>
/// Azure OpenAI implementation of AI summary generation service.
/// Analyzes backlinks to create comprehensive summaries of campaign entities.
/// </summary>
public class AISummaryService : IAISummaryService
{
    private readonly ChronicisDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AISummaryService> _logger;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly ChatClient _chatClient;

    private const decimal InputTokenCostPer1K = 0.00040m;
    private const decimal OutputTokenCostPer1K = 0.00176m;
    private const int CharsPerToken = 4;

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

    public async Task<SummaryEstimateDto> EstimateCostAsync(Guid articleId)
    {
        var article = await _context.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == articleId);

        if (article == null)
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        var backlinks = await GetBacklinksContentAsync(articleId);

        var promptTemplate = _configuration["AzureOpenAI:SummaryPromptTemplate"] ?? string.Empty;
        var backlinksContent = string.Join("\n\n", backlinks.Select(b =>
            $"--- From: {b.Title} ---\n{b.Content}"));

        var fullPrompt = promptTemplate
            .Replace("{ArticleTitle}", article.Title)
            .Replace("{BacklinkContent}", backlinksContent);

        int estimatedInputTokens = fullPrompt.Length / CharsPerToken;
        int estimatedOutputTokens = int.Parse(_configuration["AzureOpenAI:MaxOutputTokens"] ?? "1500");

        decimal estimatedCost =
            (estimatedInputTokens / 1000m * InputTokenCostPer1K) +
            (estimatedOutputTokens / 1000m * OutputTokenCostPer1K);

        return new SummaryEstimateDto
        {
            ArticleId = articleId,
            ArticleTitle = article.Title,
            BacklinkCount = backlinks.Count,
            EstimatedInputTokens = estimatedInputTokens,
            EstimatedOutputTokens = estimatedOutputTokens,
            EstimatedCostUSD = Math.Round(estimatedCost, 4),
            HasExistingSummary = !string.IsNullOrEmpty(article.AISummary),
            ExistingSummaryDate = article.AISummaryGeneratedAt
        };
    }

    public async Task<SummaryGenerationDto> GenerateSummaryAsync(Guid articleId, int maxOutputTokens = 1500)
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

            var backlinks = await GetBacklinksContentAsync(articleId);

            if (backlinks.Count == 0)
            {
                return new SummaryGenerationDto
                {
                    Success = false,
                    ErrorMessage = "No backlinks found. This article is not referenced by any other articles yet."
                };
            }

            var promptTemplate = _configuration["AzureOpenAI:SummaryPromptTemplate"]
                ?? throw new InvalidOperationException("SummaryPromptTemplate not configured");

            var backlinksContent = string.Join("\n\n", backlinks.Select(b =>
                $"--- From: {b.Title} ---\n{b.Content}\n---"));

            var prompt = promptTemplate
                .Replace("{ArticleTitle}", article.Title)
                .Replace("{BacklinkContent}", backlinksContent);

            var maxInputTokens = int.Parse(_configuration["AzureOpenAI:MaxInputTokens"] ?? "8000");
            if (prompt.Length / CharsPerToken > maxInputTokens)
            {
                _logger.LogWarning("Prompt exceeds max input tokens, truncating content");
                var maxContentLength = maxInputTokens * CharsPerToken - (promptTemplate.Length - "{BacklinkContent}".Length);
                backlinksContent = backlinksContent.Substring(0, Math.Min(backlinksContent.Length, maxContentLength));
                prompt = promptTemplate
                    .Replace("{ArticleTitle}", article.Title)
                    .Replace("{BacklinkContent}", backlinksContent);
            }

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

            var completion = await _chatClient.CompleteChatAsync(messages, chatOptions);

            var summary = completion.Value.Content[0].Text;
            var tokensUsed = completion.Value.Usage.TotalTokenCount;
            var inputTokens = completion.Value.Usage.InputTokenCount;
            var outputTokens = completion.Value.Usage.OutputTokenCount;

            var actualCost =
                (inputTokens / 1000m * InputTokenCostPer1K) +
                (outputTokens / 1000m * OutputTokenCostPer1K);

            article.AISummary = summary;
            article.AISummaryGeneratedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new SummaryGenerationDto
            {
                Success = true,
                Summary = summary,
                GeneratedDate = article.AISummaryGeneratedAt.Value,
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

    private async Task<List<(string Title, string Content)>> GetBacklinksContentAsync(Guid articleId)
    {
        // Without hashtags, there are no backlinks
        return new List<(string, string)>();
    }
}
