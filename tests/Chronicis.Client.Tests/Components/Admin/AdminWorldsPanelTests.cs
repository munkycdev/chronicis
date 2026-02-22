using Chronicis.Client.Components.Admin;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Admin;

public class AdminWorldsPanelTests : MudBlazorTestContext
{
    private IAdminApiService SetupAdminApi(
        List<AdminWorldSummaryDto>? worlds = null)
    {
        var adminApi = Substitute.For<IAdminApiService>();
        adminApi.GetWorldSummariesAsync()
            .Returns(worlds ?? new List<AdminWorldSummaryDto>());
        Services.AddSingleton(adminApi);
        Services.AddSingleton(Substitute.For<ISnackbar>());
        Services.AddSingleton(Substitute.For<ILogger<AdminWorldsPanel>>());
        return adminApi;
    }

    // ── Loading state ──────────────────────────────────────────────

    [Fact]
    public void Panel_ShowsLoadingBar_WhileLoading()
    {
        var adminApi = Substitute.For<IAdminApiService>();
        var tcs = new TaskCompletionSource<List<AdminWorldSummaryDto>>();
        adminApi.GetWorldSummariesAsync().Returns(tcs.Task);
        Services.AddSingleton(adminApi);
        Services.AddSingleton(Substitute.For<ISnackbar>());
        Services.AddSingleton(Substitute.For<ILogger<AdminWorldsPanel>>());

        var cut = RenderComponent<AdminWorldsPanel>();

        Assert.Contains("mud-progress-linear", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── Empty state ────────────────────────────────────────────────

    [Fact]
    public void Panel_ShowsNoRecordsText_WhenNoWorlds()
    {
        SetupAdminApi(new List<AdminWorldSummaryDto>());
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<AdminWorldsPanel>();

        Assert.Contains("No worlds found", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── Populated table ────────────────────────────────────────────

    [Fact]
    public void Panel_ShowsWorldRows_WhenWorldsExist()
    {
        var worlds = new List<AdminWorldSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Forgotten Realms", OwnerName = "Dave",
                    OwnerEmail = "dave@test.com", CampaignCount = 2, ArcCount = 5, ArticleCount = 100 },
            new() { Id = Guid.NewGuid(), Name = "Eberron", OwnerName = "Alice",
                    OwnerEmail = "alice@test.com" }
        };
        SetupAdminApi(worlds);
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<AdminWorldsPanel>();

        Assert.Contains("Forgotten Realms", cut.Markup);
        Assert.Contains("Eberron", cut.Markup);
        Assert.Contains("dave@test.com", cut.Markup);
    }

    // ── Error state ────────────────────────────────────────────────

    [Fact]
    public void Panel_ShowsError_WhenLoadFails()
    {
        var adminApi = Substitute.For<IAdminApiService>();
        adminApi.GetWorldSummariesAsync().Returns(Task.FromException<List<AdminWorldSummaryDto>>(new InvalidOperationException("boom")));
        Services.AddSingleton(adminApi);
        Services.AddSingleton(Substitute.For<ISnackbar>());
        Services.AddSingleton(Substitute.For<ILogger<AdminWorldsPanel>>());

        var cut = RenderComponent<AdminWorldsPanel>();

        Assert.Contains("Failed to load worlds", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
