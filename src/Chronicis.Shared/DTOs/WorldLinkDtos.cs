namespace Chronicis.Shared.DTOs;

/// <summary>
/// DTO for reading a world link
/// </summary>
public class WorldLinkDto
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new world link
/// </summary>
public class WorldLinkCreateDto
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating an existing world link
/// </summary>
public class WorldLinkUpdateDto
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}
