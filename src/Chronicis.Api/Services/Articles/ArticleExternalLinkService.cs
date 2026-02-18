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
    /// Regex to find span elements with data-type="external-link".
    /// Captures the full attribute block so individual data-* attributes can be extracted.
    /// </summary>
    [GeneratedRegex(
        @"<span\s([^>]*data-type=""external-link""[^>]*)>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExternalLinkSpanRegex();

    /// <summary>Extracts data-source value from an attribute string.</summary>
    [GeneratedRegex(@"data-source=""([^""]*)""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DataSourceRegex();

    /// <summary>Extracts data-id value from an attribute string.</summary>
    [GeneratedRegex(@"data-id=""([^""]*)""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DataIdRegex();

    /// <summary>Extracts data-title value from an attribute string.</summary>
    [GeneratedRegex(@"data-title=""([^""]*)""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DataTitleRegex();

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
    /// Attribute order within the span is not significant.
    /// </summary>
    private List<(string Source, string ExternalId, string DisplayTitle)> ExtractExternalLinksFromHtml(string? htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return new List<(string, string, string)>();
        }

        var links = new List<(string Source, string ExternalId, string DisplayTitle)>();
        var spanMatches = ExternalLinkSpanRegex().Matches(htmlContent);

        foreach (Match spanMatch in spanMatches)
        {
            if (!spanMatch.Success) continue;

            var attrs = spanMatch.Groups[1].Value;

            var sourceMatch = DataSourceRegex().Match(attrs);
            var idMatch = DataIdRegex().Match(attrs);
            var titleMatch = DataTitleRegex().Match(attrs);

            var source = sourceMatch.Success ? sourceMatch.Groups[1].Value : string.Empty;
            var externalId = idMatch.Success ? idMatch.Groups[1].Value : string.Empty;
            var displayTitle = titleMatch.Success ? titleMatch.Groups[1].Value : string.Empty;

            if (!string.IsNullOrWhiteSpace(source) &&
                !string.IsNullOrWhiteSpace(externalId) &&
                !string.IsNullOrWhiteSpace(displayTitle))
            {
                links.Add((source, externalId, displayTitle));
            }
        }

        return links;
    }
}
