using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class PromptServiceBranchCoverageTests
{
    [Fact]
    public void AddGeneralTips_ReturnsImmediately_WhenPromptLimitAlreadyReached()
    {
        var service = new PromptService(NullLogger<PromptService>.Instance);
        var addGeneralTips = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(PromptService), "AddGeneralTips");
        var dashboard = new DashboardDto();
        var prompts = new List<PromptDto>
        {
            new() { Key = "p1", Priority = 1 },
            new() { Key = "p2", Priority = 2 },
            new() { Key = "p3", Priority = 3 }
        };

        addGeneralTips.Invoke(service, [dashboard, prompts]);

        Assert.Equal(3, prompts.Count);
        Assert.DoesNotContain(prompts, p => p.Key == "tip-keyboard-shortcuts");
    }
}
