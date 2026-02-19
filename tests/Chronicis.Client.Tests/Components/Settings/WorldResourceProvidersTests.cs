using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Settings;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
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
