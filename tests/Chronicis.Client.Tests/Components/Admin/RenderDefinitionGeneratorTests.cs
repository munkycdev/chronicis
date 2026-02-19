using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Admin;
using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Admin;

[ExcludeFromCodeCoverage]
public class RenderDefinitionGeneratorTests : MudBlazorTestContext
{
    private readonly IExternalLinkApiService _externalApi = Substitute.For<IExternalLinkApiService>();
    private readonly IRenderDefinitionService _renderService = Substitute.For<IRenderDefinitionService>();
    private readonly IMarkdownService _markdownService = Substitute.For<IMarkdownService>();
    private readonly ILogger<RenderDefinitionGenerator> _logger = NullLogger<RenderDefinitionGenerator>.Instance;

    [Fact]
    public async Task SearchRecords_WhenQueryTooShort_ReturnsEmpty()
    {
        var instance = CreateInstance();
        var result = await InvokePrivateAsync<IEnumerable<string>>(instance, "SearchRecords", "a", CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchRecords_WhenSuccess_ReturnsIds()
    {
        var instance = CreateInstance();
        _externalApi.GetSuggestionsAsync(null, "ros", "dragon", Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto>
            {
                new() { Id = "bestiary/dragon/red" },
                new() { Id = "bestiary/dragon/blue" }
            });

        var result = await InvokePrivateAsync<IEnumerable<string>>(instance, "SearchRecords", "dragon", CancellationToken.None);

        Assert.Contains("bestiary/dragon/red", result);
        Assert.Contains("bestiary/dragon/blue", result);
    }

    [Fact]
    public async Task SearchRecords_WhenCanceled_ReturnsEmpty()
    {
        var instance = CreateInstance();
        _externalApi.GetSuggestionsAsync(null, "ros", "dragon", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<List<ExternalLinkSuggestionDto>>(new OperationCanceledException()));

        var result = await InvokePrivateAsync<IEnumerable<string>>(instance, "SearchRecords", "dragon", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchRecords_WhenApiThrows_ReturnsEmpty()
    {
        var instance = CreateInstance();
        _externalApi.GetSuggestionsAsync(null, "ros", "dragon", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<List<ExternalLinkSuggestionDto>>(new InvalidOperationException("boom")));

        var result = await InvokePrivateAsync<IEnumerable<string>>(instance, "SearchRecords", "dragon", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public void SuggestedFilePath_WhenRecordIdEmpty_ReturnsEmpty()
    {
        var instance = CreateInstance();
        SetField(instance, "_recordId", "");

        var value = GetPrivateProperty<string>(instance, "SuggestedFilePath");

        Assert.Equal("", value);
    }

    [Fact]
    public void SuggestedFilePath_WhenRecordIdHasCategory_ReturnsExpectedPath()
    {
        var instance = CreateInstance();
        SetField(instance, "_selectedSource", "srd");
        SetField(instance, "_recordId", "spells/acid-arrow");

        var value = GetPrivateProperty<string>(instance, "SuggestedFilePath");

        Assert.Equal("wwwroot/render-definitions/srd/spells.json", value);
    }

    [Fact]
    public async Task OnRecordSelected_WhenNull_ClearsRecordId()
    {
        var instance = CreateInstance();
        SetField(instance, "_recordId", "existing");

        await InvokePrivateAsyncWithNull(instance, "OnRecordSelected");

        Assert.Equal("", GetField<string>(instance, "_recordId"));
    }

    [Fact]
    public async Task OnRecordSelected_WhenValueProvided_LoadsRecord()
    {
        RegisterServices();
        _ = RenderComponent<MudPopoverProvider>();
        _externalApi.GetContentAsync("ros", "bestiary/dragon/red", Arg.Any<CancellationToken>())
            .Returns(new ExternalLinkContentDto
            {
                Source = "ros",
                Id = "bestiary/dragon/red",
                Title = "Red Dragon",
                JsonData = "{\"name\":\"Red Dragon\"}"
            });
        _renderService.ResolveAsync("ros", "bestiary/dragon")
            .Returns(new RenderDefinition());

        var cut = RenderComponent<RenderDefinitionGenerator>();

        await InvokePrivateOnRendererAsync(cut, "OnRecordSelected", "bestiary/dragon/red");

        Assert.NotNull(GetField<ExternalLinkContentDto?>(cut.Instance, "_sampleContent"));
        Assert.Equal("bestiary/dragon/red", GetField<string>(cut.Instance, "_recordId"));
    }

    [Fact]
    public async Task LoadSampleRecord_WhenRecordIdEmpty_SetsValidationError()
    {
        var instance = CreateInstance();
        SetField(instance, "_recordId", "");

        await InvokePrivateAsync(instance, "LoadSampleRecord");

        Assert.Equal("Please enter a record ID.", GetField<string?>(instance, "_loadError"));
    }

    [Fact]
    public async Task LoadSampleRecord_WhenNotFound_SetsNotFoundError()
    {
        RegisterServices();
        _ = RenderComponent<MudPopoverProvider>();
        _externalApi.GetContentAsync("ros", "bestiary/dragon/red", Arg.Any<CancellationToken>())
            .Returns((ExternalLinkContentDto?)null);
        var cut = RenderComponent<RenderDefinitionGenerator>();
        SetField(cut.Instance, "_selectedSource", "ros");
        SetField(cut.Instance, "_recordId", "bestiary/dragon/red");

        await InvokePrivateOnRendererAsync(cut, "LoadSampleRecord");

        Assert.Contains("Record not found", GetField<string?>(cut.Instance, "_loadError"));
    }

    [Fact]
    public async Task LoadSampleRecord_WhenJsonMissing_SetsJsonDataError()
    {
        RegisterServices();
        _ = RenderComponent<MudPopoverProvider>();
        _externalApi.GetContentAsync("ros", "bestiary/dragon/red", Arg.Any<CancellationToken>())
            .Returns(new ExternalLinkContentDto
            {
                Source = "ros",
                Id = "bestiary/dragon/red",
                Title = "Red Dragon",
                JsonData = ""
            });
        var cut = RenderComponent<RenderDefinitionGenerator>();
        SetField(cut.Instance, "_selectedSource", "ros");
        SetField(cut.Instance, "_recordId", "bestiary/dragon/red");

        await InvokePrivateOnRendererAsync(cut, "LoadSampleRecord");

        Assert.Contains("no JsonData", GetField<string?>(cut.Instance, "_loadError"));
        Assert.NotNull(GetField<ExternalLinkContentDto?>(cut.Instance, "_sampleContent"));
    }

    [Fact]
    public async Task LoadSampleRecord_WhenSuccess_LoadsSampleAndStatus()
    {
        RegisterServices();
        _ = RenderComponent<MudPopoverProvider>();
        _externalApi.GetContentAsync("ros", "bestiary/dragon/red", Arg.Any<CancellationToken>())
            .Returns(new ExternalLinkContentDto
            {
                Source = "ros",
                Id = "bestiary/dragon/red",
                Title = "Red Dragon",
                JsonData = "{\"name\":\"Red Dragon\"}"
            });
        _renderService.ResolveAsync("ros", "bestiary/dragon")
            .Returns(new RenderDefinition());

        var cut = RenderComponent<RenderDefinitionGenerator>();
        SetField(cut.Instance, "_selectedSource", "ros");
        SetField(cut.Instance, "_recordId", "bestiary/dragon/red");

        await InvokePrivateOnRendererAsync(cut, "LoadSampleRecord");

        Assert.NotNull(GetField<ExternalLinkContentDto?>(cut.Instance, "_sampleContent"));
        Assert.Equal("No custom definition found — use Auto-Generate to create one.", GetField<string?>(cut.Instance, "_existingDefStatus"));
        Assert.False(GetField<bool>(cut.Instance, "_isLoading"));
    }

    [Fact]
    public async Task LoadSampleRecord_WhenApiThrows_SetsLoadError()
    {
        RegisterServices();
        _ = RenderComponent<MudPopoverProvider>();
        _externalApi.GetContentAsync("ros", "bestiary/dragon/red", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ExternalLinkContentDto?>(new InvalidOperationException("boom")));

        var cut = RenderComponent<RenderDefinitionGenerator>();
        SetField(cut.Instance, "_selectedSource", "ros");
        SetField(cut.Instance, "_recordId", "bestiary/dragon/red");

        await InvokePrivateOnRendererAsync(cut, "LoadSampleRecord");

        var error = GetField<string?>(cut.Instance, "_loadError");
        Assert.NotNull(error);
        Assert.Contains("Failed to load record:", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryLoadExistingDefinition_WhenSampleIsNull_ReturnsWithoutChanges()
    {
        var instance = CreateInstance();

        await InvokePrivateAsync(instance, "TryLoadExistingDefinition");

        Assert.Null(GetField<string?>(instance, "_existingDefStatus"));
    }

    [Fact]
    public async Task TryLoadExistingDefinition_WhenCustomDefinitionExists_SetsDefinitionAndStatus()
    {
        var instance = CreateInstance();
        SetField(instance, "_sampleContent", new ExternalLinkContentDto
        {
            Source = "ros",
            Id = "bestiary/dragon/red",
            Title = "Red Dragon",
            JsonData = "{\"name\":\"Red Dragon\"}"
        });
        _renderService.ResolveAsync("ros", "bestiary/dragon")
            .Returns(new RenderDefinition
            {
                DisplayName = "Dragon",
                Sections = new List<RenderSection> { new() { Label = "Stats" } }
            });

        await InvokePrivateAsync(instance, "TryLoadExistingDefinition");

        Assert.NotNull(GetField<RenderDefinition?>(instance, "_activeDefinition"));
        Assert.Contains("Loaded existing definition", GetField<string?>(instance, "_existingDefStatus"));
        Assert.False(GetField<bool>(instance, "_definitionDirty"));
    }

    [Fact]
    public async Task TryLoadExistingDefinition_WhenResolveThrows_SetsErrorStatus()
    {
        var instance = CreateInstance();
        SetField(instance, "_sampleContent", new ExternalLinkContentDto
        {
            Source = "ros",
            Id = "bestiary/dragon/red",
            Title = "Red Dragon",
            JsonData = "{\"name\":\"Red Dragon\"}"
        });
        _renderService.ResolveAsync("ros", "bestiary/dragon")
            .Returns(_ => Task.FromException<RenderDefinition>(new InvalidOperationException("boom")));

        await InvokePrivateAsync(instance, "TryLoadExistingDefinition");

        Assert.Equal("Could not check for existing definition.", GetField<string?>(instance, "_existingDefStatus"));
    }

    [Fact]
    public void AutoGenerate_WhenNoSampleContent_DoesNothing()
    {
        var instance = CreateInstance();

        InvokePrivate(instance, "AutoGenerate");

        Assert.Equal("", GetField<string>(instance, "_definitionJson"));
        Assert.Null(GetField<string?>(instance, "_jsonError"));
    }

    [Fact]
    public void AutoGenerate_WhenValidJson_GeneratesDefinition()
    {
        var instance = CreateInstance();
        SetField(instance, "_sampleContent", new ExternalLinkContentDto
        {
            Source = "ros",
            Id = "bestiary/dragon/red",
            Title = "Red Dragon",
            JsonData = "{\"name\":\"Red Dragon\",\"description\":\"A dragon.\"}"
        });

        InvokePrivate(instance, "AutoGenerate");

        var json = GetField<string>(instance, "_definitionJson");
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.NotNull(GetField<RenderDefinition?>(instance, "_activeDefinition"));
        Assert.Null(GetField<string?>(instance, "_jsonError"));
    }

    [Fact]
    public void AutoGenerate_WhenInvalidJson_SetsError()
    {
        var instance = CreateInstance();
        SetField(instance, "_sampleContent", new ExternalLinkContentDto
        {
            Source = "ros",
            Id = "bestiary/dragon/red",
            Title = "Red Dragon",
            JsonData = "{invalid"
        });

        InvokePrivate(instance, "AutoGenerate");

        Assert.Contains("Generation failed:", GetField<string?>(instance, "_jsonError"));
    }

    [Fact]
    public void RendersBasicGeneratorUi()
    {
        RegisterServices();
        _ = RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<RenderDefinitionGenerator>();

        Assert.Contains("Render Definition Generator", cut.Markup);
        Assert.Contains("Select a sample record", cut.Markup);
    }

    [Fact]
    public void RendersLoadedStateSections_WhenSampleContentExists()
    {
        RegisterServices();
        _ = RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<RenderDefinitionGenerator>();
        SetField(cut.Instance, "_sampleContent", new ExternalLinkContentDto
        {
            Source = "ros",
            Id = "bestiary/dragon/red",
            Title = "Red Dragon",
            JsonData = "{\"name\":\"Red Dragon\"}"
        });
        SetField(cut.Instance, "_activeDefinition", new RenderDefinition());

        cut.Render();

        Assert.Contains("Render Definition JSON", cut.Markup);
        Assert.Contains("Preview", cut.Markup);
        Assert.Contains("Suggested file path", cut.Markup);
    }

    [Fact]
    public void RendersErrorAndStatusAlerts_WhenSet()
    {
        RegisterServices();
        _ = RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<RenderDefinitionGenerator>();
        SetField(cut.Instance, "_loadError", "Load error");
        SetField(cut.Instance, "_existingDefStatus", "Status info");

        cut.Render();

        Assert.Contains("Load error", cut.Markup);
        Assert.Contains("Status info", cut.Markup);
    }

    [Fact]
    public void OnDefinitionJsonChanged_WhenInvalidJson_SetsError()
    {
        var instance = CreateInstance();
        InvokePrivate(instance, "OnDefinitionJsonChanged", "{invalid");
        Assert.Contains("Invalid JSON:", GetField<string?>(instance, "_jsonError"));
        Assert.True(GetField<bool>(instance, "_definitionDirty"));
    }

    [Fact]
    public void OnDefinitionJsonChanged_WhenValidJson_ClearsError()
    {
        var instance = CreateInstance();
        InvokePrivate(instance, "OnDefinitionJsonChanged", "{}");
        Assert.Null(GetField<string?>(instance, "_jsonError"));
        Assert.True(GetField<bool>(instance, "_definitionDirty"));
    }

    [Fact]
    public void ApplyDefinition_WhenWhitespace_DoesNothing()
    {
        var instance = CreateInstance();
        SetField(instance, "_definitionJson", "   ");
        InvokePrivate(instance, "ApplyDefinition");
        Assert.Null(GetField<RenderDefinition?>(instance, "_activeDefinition"));
    }

    [Fact]
    public void ApplyDefinition_WhenInvalidJson_SetsError()
    {
        var instance = CreateInstance();
        SetField(instance, "_definitionJson", "{invalid");
        InvokePrivate(instance, "ApplyDefinition");
        Assert.Contains("Cannot apply", GetField<string?>(instance, "_jsonError"));
    }

    [Fact]
    public void ApplyDefinition_WhenValidJson_AppliesAndClearsDirty()
    {
        var instance = CreateInstance();
        SetField(instance, "_definitionJson", "{}");
        SetField(instance, "_definitionDirty", true);
        InvokePrivate(instance, "ApplyDefinition");

        Assert.NotNull(GetField<RenderDefinition?>(instance, "_activeDefinition"));
        Assert.False(GetField<bool>(instance, "_definitionDirty"));
        Assert.Null(GetField<string?>(instance, "_jsonError"));
    }

    private RenderDefinitionGenerator CreateInstance()
    {
        var instance = new RenderDefinitionGenerator();
        SetProperty(instance, "ExternalLinkApi", _externalApi);
        SetProperty(instance, "RenderDefService", _renderService);
        SetProperty(instance, "Logger", _logger);
        return instance;
    }

    private void RegisterServices()
    {
        Services.AddSingleton(_externalApi);
        Services.AddSingleton(_renderService);
        Services.AddSingleton(_markdownService);
        _markdownService.ToHtml(Arg.Any<string>()).Returns(call => call.Arg<string>());
        Services.AddSingleton(_logger);
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static T GetPrivateProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return (T)property!.GetValue(instance)!;
    }

    private static object? InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(instance, args);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var result = InvokePrivate(instance, methodName, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static async Task InvokePrivateAsyncWithNull(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(instance, [null]);
        if (result is Task task)
        {
            await task;
        }
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<RenderDefinitionGenerator> cut, string methodName, params object[] args)
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

    private static async Task<T> InvokePrivateAsync<T>(object instance, string methodName, params object[] args)
    {
        var result = InvokePrivate(instance, methodName, args);
        Assert.NotNull(result);

        if (result is Task<T> typedTask)
        {
            return await typedTask;
        }

        if (result is Task task)
        {
            await task;
            var taskType = task.GetType();
            var resultProperty = taskType.GetProperty("Result");
            Assert.NotNull(resultProperty);
            return (T)resultProperty!.GetValue(task)!;
        }

        return (T)result;
    }
}
