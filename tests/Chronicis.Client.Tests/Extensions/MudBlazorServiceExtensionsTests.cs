using Chronicis.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Extensions;

public class MudBlazorServiceExtensionsTests
{
    [Fact]
    public void AddChronicisMudBlazor_ConfiguresSnackbarDefaults()
    {
        var services = new ServiceCollection();

        var returned = services.AddChronicisMudBlazor();

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<IOptions<SnackbarConfiguration>>().Value;

        Assert.Equal(Defaults.Classes.Position.BottomRight, config.PositionClass);
        Assert.False(config.PreventDuplicates);
        Assert.True(config.NewestOnTop);
        Assert.True(config.ShowCloseIcon);
        Assert.Equal(4000, config.VisibleStateDuration);
        Assert.Equal(300, config.HideTransitionDuration);
        Assert.Equal(300, config.ShowTransitionDuration);
        Assert.Equal(Variant.Filled, config.SnackbarVariant);
    }
}

