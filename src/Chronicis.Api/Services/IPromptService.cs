using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for generating contextual prompts based on user state.
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Generate prompts based on the user's dashboard data.
    /// </summary>
    List<PromptDto> GeneratePrompts(DashboardDto dashboard);
}
