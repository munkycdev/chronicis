using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Settings;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Settings;

[ExcludeFromCodeCoverage]
public class WorldResourceProvidersTests : MudBlazorTestContext
{
    private readonly IResourceProviderApiService _providersApi = Substitute.For<IResourceProviderApiService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly ILogger<WorldResourceProviders> _logger = NullLogger<WorldResourceProviders>.Instance;

    public WorldResourceProvidersTests()
    {
        Services.AddSingleton(_providersApi);
        Services.AddSingleton(_snackbar);
        Services.AddSingleton(_logger);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _snackbar.Dispose();
        }

        base.Dispose(disposing);
    }

    [Fact]
    public void OnParametersSetAsync_LoadsAndSortsProviders()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("open5e", "Open5e", enabled: false),
            CreateProvider("srd", "SRD", enabled: true)
        });

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));

        cut.WaitForAssertion(() =>
        {
            var providers = GetField<List<WorldResourceProviderDto>?>(cut.Instance, "_providers");
            Assert.NotNull(providers);
            Assert.Equal("Open5e", providers![0].Provider.Name);
            Assert.Equal("SRD", providers[1].Provider.Name);
            Assert.False(GetField<bool>(cut.Instance, "_loading"));
            Assert.False(GetField<bool>(cut.Instance, "_error"));
        });
    }

    [Fact]
    public void OnParametersSetAsync_WhenNullResponse_SetsError()
    {
        _providersApi.GetWorldProvidersAsync(Arg.Any<Guid>()).Returns((List<WorldResourceProviderDto>?)null);

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        cut.WaitForAssertion(() =>
        {
            Assert.True(GetField<bool>(cut.Instance, "_error"));
            Assert.False(GetField<bool>(cut.Instance, "_loading"));
        });
    }

    [Fact]
    public void OnParametersSetAsync_WhenServiceThrows_SetsError()
    {
        _providersApi.GetWorldProvidersAsync(Arg.Any<Guid>())
            .Returns(_ => Task.FromException<List<WorldResourceProviderDto>?>(new InvalidOperationException("boom")));

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        cut.WaitForAssertion(() =>
        {
            Assert.True(GetField<bool>(cut.Instance, "_error"));
            Assert.False(GetField<bool>(cut.Instance, "_loading"));
        });
    }

    [Fact]
    public async Task OnToggleProvider_WhenSuccess_UpdatesLocalState()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("srd", "SRD", enabled: false)
        });
        _providersApi.ToggleProviderAsync(worldId, "srd", true).Returns(true);

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));

        await InvokePrivateOnRendererAsync(cut, "OnToggleProvider", "srd", true);

        var providers = GetField<List<WorldResourceProviderDto>?>(cut.Instance, "_providers");
        Assert.NotNull(providers);
        Assert.True(providers![0].IsEnabled);
        Assert.False(GetField<bool>(cut.Instance, "_updating"));
    }

    [Fact]
    public async Task OnToggleProvider_WhenFailure_DoesNotChangeLocalState()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("srd", "SRD", enabled: false)
        });
        _providersApi.ToggleProviderAsync(worldId, "srd", true).Returns(false);

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));

        await InvokePrivateOnRendererAsync(cut, "OnToggleProvider", "srd", true);

        var providers = GetField<List<WorldResourceProviderDto>?>(cut.Instance, "_providers");
        Assert.NotNull(providers);
        Assert.False(providers![0].IsEnabled);
        Assert.False(GetField<bool>(cut.Instance, "_updating"));
    }

    [Fact]
    public async Task OnToggleProvider_WhenProviderMissing_StillShowsSuccessMessage()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("srd", "SRD", enabled: false)
        });
        _providersApi.ToggleProviderAsync(worldId, "open5e", true).Returns(true);

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));

        await InvokePrivateOnRendererAsync(cut, "OnToggleProvider", "open5e", true);

        _snackbar.Received().Add("open5e enabled successfully", Severity.Success);
    }

    [Fact]
    public async Task OnToggleProvider_WhenException_ShowsErrorAndResetsUpdating()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("srd", "SRD", enabled: false)
        });
        _providersApi.ToggleProviderAsync(worldId, "srd", true)
            .Returns(_ => Task.FromException<bool>(new InvalidOperationException("toggle-failed")));

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));

        await InvokePrivateOnRendererAsync(cut, "OnToggleProvider", "srd", true);

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Error updating provider: toggle-failed")), Severity.Error);
        Assert.False(GetField<bool>(cut.Instance, "_updating"));
    }

    [Fact]
    public async Task OnToggleProvider_WhenDisablingFailure_ShowsDisableMessage()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("srd", "SRD", enabled: true)
        });
        _providersApi.ToggleProviderAsync(worldId, "srd", false).Returns(false);

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));

        await InvokePrivateOnRendererAsync(cut, "OnToggleProvider", "srd", false);

        _snackbar.Received().Add("Failed to disable srd", Severity.Error);
    }

    [Fact]
    public async Task OnToggleProvider_WhenDisablingSuccess_ShowsDisabledMessage()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("srd", "SRD", enabled: true)
        });
        _providersApi.ToggleProviderAsync(worldId, "srd", false).Returns(true);

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));

        await InvokePrivateOnRendererAsync(cut, "OnToggleProvider", "srd", false);

        _snackbar.Received().Add("srd disabled successfully", Severity.Success);
    }

    [Fact]
    public void Render_WhenLoading_ShowsProgressBar()
    {
        var pending = new TaskCompletionSource<List<WorldResourceProviderDto>?>();
        _providersApi.GetWorldProvidersAsync(Arg.Any<Guid>()).Returns(pending.Task);

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("mud-progress-linear", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_WhenProvidersLoaded_ShowsProviderDetailsAndSwitch()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("srd", "SRD", enabled: false)
        });

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("SRD", cut.Markup);
            Assert.Contains("Documentation", cut.Markup);
            Assert.Contains("License", cut.Markup);
            Assert.NotEmpty(cut.FindAll("input[type='checkbox']"));
        });
    }

    [Fact]
    public async Task Render_SwitchToggle_InvokesValueChangedHandler()
    {
        var worldId = Guid.NewGuid();
        _providersApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            CreateProvider("srd", "SRD", enabled: false)
        });
        _providersApi.ToggleProviderAsync(worldId, "srd", true).Returns(true);

        var cut = RenderComponent<WorldResourceProviders>(p => p.Add(x => x.WorldId, worldId));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[type='checkbox']")));

        cut.Find("input[type='checkbox']").Change(new ChangeEventArgs { Value = true });

        await _providersApi.Received(1).ToggleProviderAsync(worldId, "srd", true);
    }

    private static WorldResourceProviderDto CreateProvider(string code, string name, bool enabled)
    {
        return new WorldResourceProviderDto
        {
            IsEnabled = enabled,
            Provider = new ResourceProviderDto
            {
                Code = code,
                Name = name,
                Description = $"{name} desc",
                DocumentationLink = "https://example.com/docs",
                License = "https://example.com/license"
            }
        };
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<WorldResourceProviders> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);

            if (result is Task task)
            {
                await task;
            }
        });
    }
}
