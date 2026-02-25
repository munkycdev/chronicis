using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Tutorial content payload returned for a resolved page context.
/// </summary>
[ExcludeFromCodeCoverage]
public class TutorialDto
{
    public Guid ArticleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// SysAdmin tutorial mapping row for listing and edit flows.
/// </summary>
[ExcludeFromCodeCoverage]
public class TutorialMappingDto
{
    public Guid Id { get; set; }

    public string PageType { get; set; } = string.Empty;

    public string PageTypeName { get; set; } = string.Empty;

    public Guid ArticleId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// SysAdmin request to create a tutorial mapping, optionally creating a new tutorial article.
/// </summary>
[ExcludeFromCodeCoverage]
public class TutorialMappingCreateDto
{
    public string PageType { get; set; } = string.Empty;

    public string PageTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Attach an existing tutorial article when provided; otherwise a new tutorial article is created.
    /// </summary>
    public Guid? ArticleId { get; set; }

    /// <summary>
    /// Used when creating a new tutorial article.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Used when creating a new tutorial article.
    /// </summary>
    public string? Body { get; set; }
}

/// <summary>
/// SysAdmin request to update an existing tutorial mapping.
/// </summary>
[ExcludeFromCodeCoverage]
public class TutorialMappingUpdateDto
{
    public string PageType { get; set; } = string.Empty;

    public string PageTypeName { get; set; } = string.Empty;

    public Guid ArticleId { get; set; }
}
