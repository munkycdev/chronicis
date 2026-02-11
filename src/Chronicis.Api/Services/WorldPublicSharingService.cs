using System.Text.RegularExpressions;
using Chronicis.Api.Data;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for world public sharing and slug management
/// </summary>
public class WorldPublicSharingService : IWorldPublicSharingService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<WorldPublicSharingService> _logger;

    // Regex for valid public slug: lowercase alphanumeric with hyphens, no leading/trailing hyphens
    private static readonly Regex PublicSlugRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    // Reserved slugs that shouldn't be used
    private static readonly string[] ReservedSlugs = 
        { "api", "admin", "public", "private", "new", "edit", "delete", "search", "login", "logout", "settings" };

    public WorldPublicSharingService(ChronicisDbContext context, ILogger<WorldPublicSharingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsPublicSlugAvailableAsync(string publicSlug, Guid? excludeWorldId = null)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        var query = _context.Worlds
            .AsNoTracking()
            .Where(w => w.PublicSlug == normalizedSlug);

        if (excludeWorldId.HasValue)
        {
            query = query.Where(w => w.Id != excludeWorldId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<PublicSlugCheckResultDto> CheckPublicSlugAsync(string slug, Guid? excludeWorldId = null)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        // Validate format first
        var validationError = ValidatePublicSlug(normalizedSlug);
        if (validationError != null)
        {
            return new PublicSlugCheckResultDto
            {
                IsAvailable = false,
                ValidationError = validationError,
                SuggestedSlug = GenerateSuggestedSlug(slug)
            };
        }

        // Check availability
        var isAvailable = await IsPublicSlugAvailableAsync(normalizedSlug, excludeWorldId);
        
        return new PublicSlugCheckResultDto
        {
            IsAvailable = isAvailable,
            ValidationError = null,
            SuggestedSlug = isAvailable ? null : await GenerateAvailableSlugAsync(normalizedSlug)
        };
    }

    public async Task<WorldDto?> GetWorldByPublicSlugAsync(string publicSlug)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        var world = await _context.Worlds
            .AsNoTracking()
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .FirstOrDefaultAsync(w => w.PublicSlug == normalizedSlug && w.IsPublic);

        if (world == null)
            return null;

        return new WorldDto
        {
            Id = world.Id,
            Name = world.Name,
            Slug = world.Slug,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            CampaignCount = world.Campaigns?.Count ?? 0,
            MemberCount = world.Members?.Count ?? 0,
            IsPublic = world.IsPublic,
            PublicSlug = world.PublicSlug
        };
    }

    /// <summary>
    /// Validate a public slug format.
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    public string? ValidatePublicSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "Public slug is required";

        if (slug.Length < 3)
            return "Public slug must be at least 3 characters";

        if (slug.Length > 100)
            return "Public slug must be 100 characters or less";

        if (!PublicSlugRegex.IsMatch(slug))
            return "Public slug must contain only lowercase letters, numbers, and hyphens (no leading/trailing hyphens)";

        if (ReservedSlugs.Contains(slug))
            return "This slug is reserved and cannot be used";

        return null;
    }

    /// <summary>
    /// Generate a suggested slug from user input.
    /// </summary>
    private static string GenerateSuggestedSlug(string input)
    {
        var slug = input.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[\s_]+", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (slug.Length < 3)
            slug = slug.PadRight(3, '0');

        return slug;
    }

    /// <summary>
    /// Generate an available slug by appending numbers.
    /// </summary>
    private async Task<string> GenerateAvailableSlugAsync(string baseSlug)
    {
        var suffix = 1;
        var candidate = $"{baseSlug}-{suffix}";

        while (!await IsPublicSlugAvailableAsync(candidate))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";

            if (suffix > 100)
                return $"{baseSlug}-{Guid.NewGuid().ToString()[..8]}";
        }

        return candidate;
    }
}
