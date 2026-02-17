using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

/// <summary>
/// Information about a claimed character.
/// </summary>
[ExcludeFromCodeCoverage]
public class ClaimedCharacterDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? IconEmoji { get; set; }
    public Guid WorldId { get; set; }
    public string WorldName { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to claim a character.
/// </summary>
[ExcludeFromCodeCoverage]
public class ClaimCharacterDto
{
    // Empty for now - just marks the character as claimed by the current user
}
