using Chronicis.Client.Components.Admin;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;
using UtilitiesPage = Chronicis.Client.Pages.Admin.Utilities;

namespace Chronicis.Client.Tests.Pages.Admin;

public class UtilitiesTests : MudBlazorTestContext
{
    [Fact]
    public void Utilities_Unauthorized_ShowsPermissionError()
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        adminAuth.IsSysAdminAsync().Returns(false);
        Services.AddSingleton(adminAuth);

        var cut = RenderComponent<UtilitiesPage>();

        Assert.Contains("do not have permission", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Utilities_Authorized_ShowsPageContent()
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        adminAuth.IsSysAdminAsync().Returns(true);

        Services.AddSingleton(adminAuth);

        // Dependencies for the Worlds tab (AdminWorldsPanel)
        var adminApi = Substitute.For<IAdminApiService>();
        adminApi.GetWorldSummariesAsync().Returns(new List<AdminWorldSummaryDto>());
        Services.AddSingleton(adminApi);
        Services.AddSingleton(Substitute.For<ISnackbar>());
        Services.AddSingleton(Substitute.For<ILogger<AdminWorldsPanel>>());
        Services.AddSingleton(Substitute.For<IDialogService>());

        // Dependencies for the External Resources tab (RenderDefinitionGenerator)
        Services.AddSingleton(Substitute.For<IExternalLinkApiService>());
        Services.AddSingleton(Substitute.For<IRenderDefinitionService>());
        Services.AddSingleton(Substitute.For<ILogger<RenderDefinitionGenerator>>());

        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<UtilitiesPage>();

        Assert.Contains("Admin Utilities", cut.Markup);
        Assert.Contains("System Status", cut.Markup);
        Assert.Contains("Worlds", cut.Markup);
        Assert.Contains("External Resources", cut.Markup);
    }

    [Fact]
    public void Utilities_WhileAuthorizing_ShowsLoadingBar()
    {
        var adminAuth = Substitute.For<IAdminAuthService>();
        var tcs = new TaskCompletionSource<bool>();
        adminAuth.IsSysAdminAsync().Returns(tcs.Task);
        Services.AddSingleton(adminAuth);

        var cut = RenderComponent<UtilitiesPage>();

        Assert.Contains("mud-progress-linear", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
