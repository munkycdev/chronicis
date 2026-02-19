using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class SettingsTests : MudBlazorTestContext
{
    [Fact]
    public void Settings_RendersTabs()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetCurrentUserAsync().Returns(new UserInfo { DisplayName = "User", Email = "user@example.com" });

        var worldsApi = Substitute.For<IWorldApiService>();
        worldsApi.GetWorldsAsync().Returns(new List<WorldDto>
        {
            new() { Id = Guid.NewGuid(), Name = "World One", Slug = "world-one" }
        });

        var exportApi = Substitute.For<IExportApiService>();
        exportApi.ExportWorldToMarkdownAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);

        Services.AddSingleton(auth);
        Services.AddSingleton(worldsApi);
        Services.AddSingleton(exportApi);
        Services.AddSingleton(Substitute.For<ISnackbar>());

        var cut = RenderComponent<Settings>();

        Assert.Contains("Settings", cut.Markup);
        Assert.Contains("Profile", cut.Markup);
        Assert.Contains("Data", cut.Markup);
        Assert.Contains("Preferences", cut.Markup);
    }
}
