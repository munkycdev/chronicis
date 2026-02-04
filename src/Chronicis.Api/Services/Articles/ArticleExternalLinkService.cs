using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services.Articles;

/// <summary>
/// Service for managing external resource links embedded in article content.
/// </summary>
public partial class ArticleExternalLinkService : IArticleExternalLinkService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<ArticleExternalLinkService> _logger;

    public ArticleExternalLinkService(
        ChronicisDbContext context,
        ILogger<ArticleExternalLinkService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Regex pattern to extract external links from article HTML.
    /// Matches: &lt;span data-type="external-link" ... data-source="..." data-id="..." data-title="..."&gt;
    /// </summary>
    [GeneratedRegex(
        @"<span[^>]*data-type=""external-link""[^>]*data-source=""([^""]*)""[^>]*data-id=""([^""]*)""[^>]*data-title=""([^""]*)""[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExternalLinkRegex();

    public async Task SyncExternalLinksAsync(Guid articleId, string? htmlContent)
    {
        try
        {
            // Extract external links from HTML
            var extractedLinks = ExtractExternalLinksFromHtml(htmlContent);

            _logger.LogDebug(
                "Extracted {Count} external links from article {ArticleId}",
                extractedLinks.Count,
                articleId);

            // Delete all existing external links for this article
            var existingLinks = await _context.ArticleExternalLinks
                .Where(ael => ael.ArticleId == articleId)
                .ToListAsync();

            if (existingLinks.Any())
            {
                _context.ArticleExternalLinks.RemoveRange(existingLinks);
                _logger.LogDebug(
                    "Removed {Count} existing external links for article {ArticleId}",
                    existingLinks.Count,
                    articleId);
            }

            // Insert new external links
            if (extractedLinks.Any())
            {
                var newLinks = extractedLinks.Select(link => new ArticleExternalLink
                {
                    Id = Guid.NewGuid(),
                    ArticleId = articleId,
                    Source = link.Source,
                    ExternalId = link.ExternalId,
                    DisplayTitle = link.DisplayTitle
                }).ToList();

                await _context.ArticleExternalLinks.AddRangeAsync(newLinks);
                
                _logger.LogDebug(
                    "Added {Count} new external links for article {ArticleId}",
                    newLinks.Count,
                    articleId);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error syncing external links for article {ArticleId}",
                articleId);
            throw;
        }
    }

    public async Task<List<ArticleExternalLinkDto>> GetExternalLinksForArticleAsync(Guid articleId)
    {
        try
        {
            var links = await _context.ArticleExternalLinks
                .Where(ael => ael.ArticleId == articleId)
                .OrderBy(ael => ael.Source)
                .ThenBy(ael => ael.DisplayTitle)
                .Select(ael => new ArticleExternalLinkDto
                {
                    Id = ael.Id,
                    ArticleId = ael.ArticleId,
                    Source = ael.Source,
                    ExternalId = ael.ExternalId,
                    DisplayTitle = ael.DisplayTitle
                })
                .ToListAsync();

            _logger.LogDebug(
                "Retrieved {Count} external links for article {ArticleId}",
                links.Count,
                articleId);

            return links;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving external links for article {ArticleId}",
                articleId);
            throw;
        }
    }

    /// <summary>
    /// Extracts external link information from HTML content.
    /// </summary>
    private List<(string Source, string ExternalId, string DisplayTitle)> ExtractExternalLinksFromHtml(string? htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return new List<(string, string, string)>();
        }

        var links = new List<(string Source, string ExternalId, string DisplayTitle)>();
        var matches = ExternalLinkRegex().Matches(htmlContent);

        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count >= 4)
            {
                var source = match.Groups[1].Value;
                var externalId = match.Groups[2].Value;
                var displayTitle = match.Groups[3].Value;

                // Validate that we have all required fields
                if (!string.IsNullOrWhiteSpace(source) &&
                    !string.IsNullOrWhiteSpace(externalId) &&
                    !string.IsNullOrWhiteSpace(displayTitle))
                {
                    links.Add((source, externalId, displayTitle));
                }
            }
        }

        return links;
    }
}
