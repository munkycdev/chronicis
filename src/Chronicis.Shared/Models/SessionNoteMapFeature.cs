using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Join entity linking a SessionNote article to a map feature.
/// </summary>
[ExcludeFromCodeCoverage]
public class SessionNoteMapFeature
{
    public Guid SessionNoteId { get; set; }

    public Guid MapFeatureId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Article SessionNote { get; set; } = null!;

    public MapFeature MapFeature { get; set; } = null!;
}
