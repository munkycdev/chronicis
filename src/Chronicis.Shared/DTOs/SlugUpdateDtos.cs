using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

[ExcludeFromCodeCoverage]
public class SlugUpdateRequestDto
{
    public string Slug { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public class SlugUpdateResponseDto
{
    public string Slug { get; set; } = string.Empty;
}
