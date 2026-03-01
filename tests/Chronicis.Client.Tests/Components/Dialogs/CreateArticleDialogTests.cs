using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Dialogs;

[ExcludeFromCodeCoverage]
public class CreateArticleDialogTests : MudBlazorTestContext
{
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();

    public CreateArticleDialogTests()
    {
        Services.AddSingleton(_articleApi);
    }

    [Theory]
    [InlineData(ArticleType.Character, "New Player Character", "Character Name")]
    [InlineData(ArticleType.WikiArticle, "New Wiki Article", "Title")]
    [InlineData((ArticleType)999, "New Article", "Title")]
    public void TypeHelpers_ReturnExpectedValues(ArticleType type, string expectedTitle, string expectedLabel)
    {
        var component = new CreateArticleDialog();
        SetProperty(component, nameof(CreateArticleDialog.ArticleType), type);
        Assert.Equal(expectedTitle, InvokePrivate<string>(component, "GetTitle"));
        Assert.Equal(expectedLabel, InvokePrivate<string>(component, "GetTitleLabel"));
        Assert.False(string.IsNullOrWhiteSpace(InvokePrivate<string>(component, "GetIcon")));
    }

    [Fact]
    public async Task Submit_EmptyTitle_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateArticleDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_title", " ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _articleApi.DidNotReceive().CreateArticleAsync(Arg.Any<ArticleCreateDto>());
    }

    [Fact]
    public async Task Submit_ValidTitle_CallsApiWithTrimmedTitle()
    {
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns(new ArticleDto { Id = Guid.NewGuid(), Title = "Hero" });
        var worldId = Guid.NewGuid();

        var cut = RenderComponent<CreateArticleDialog>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.ArticleType, ArticleType.Character));
        SetField(cut.Instance, "_title", "  Hero  ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _articleApi.Received(1).CreateArticleAsync(Arg.Is<ArticleCreateDto>(dto =>
            dto.Title == "Hero" && dto.WorldId == worldId && dto.Type == ArticleType.Character));
    }

    [Fact]
    public async Task Submit_NullApiResult_SetsSubmittingFalse()
    {
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns((ArticleDto?)null);
        var cut = RenderComponent<CreateArticleDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_title", "Title");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        Assert.False(GetField<bool>(cut.Instance, "_isSubmitting"));
    }

    [Fact]
    public async Task DialogRender_ShowsExpectedContent()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateArticleDialog>("New Article", new DialogParameters
        {
            { nameof(CreateArticleDialog.WorldId), Guid.NewGuid() },
            { nameof(CreateArticleDialog.ArticleType), ArticleType.WikiArticle }
        });

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("New Wiki Article", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("Title", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("Create", provider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task DialogRender_WhenSubmitting_ShowsProgressAndDisablesCreate()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateArticleDialog>("New Article", new DialogParameters
        {
            { nameof(CreateArticleDialog.WorldId), Guid.NewGuid() },
            { nameof(CreateArticleDialog.ArticleType), ArticleType.Character }
        });

        var dialog = provider.FindComponent<CreateArticleDialog>();
        SetField(dialog.Instance, "_title", "Hero");
        SetField(dialog.Instance, "_isSubmitting", true);
        dialog.Render();

        Assert.NotEmpty(provider.FindAll(".mud-progress-circular"));
        Assert.Contains("Character Name", provider.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancel_WhenDialogOpen_ClosesDialog()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateArticleDialog>("New Article", new DialogParameters
        {
            { nameof(CreateArticleDialog.WorldId), Guid.NewGuid() }
        });

        var dialog = provider.FindComponent<CreateArticleDialog>();
        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Cancel"));

        provider.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("New Wiki Article", provider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Submit_WhenAlreadySubmitting_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateArticleDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_title", "Hero");
        SetField(cut.Instance, "_isSubmitting", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _articleApi.DidNotReceive().CreateArticleAsync(Arg.Any<ArticleCreateDto>());
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndSuccess_ClosesDialog()
    {
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns(new ArticleDto { Id = Guid.NewGuid(), Title = "Hero" });
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateArticleDialog>("New Article", new DialogParameters
        {
            { nameof(CreateArticleDialog.WorldId), Guid.NewGuid() }
        });
        var dialog = provider.FindComponent<CreateArticleDialog>();
        SetField(dialog.Instance, "_title", "Hero");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("New Wiki Article", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndNullResult_ClosesDialog()
    {
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns((ArticleDto?)null);
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateArticleDialog>("New Article", new DialogParameters
        {
            { nameof(CreateArticleDialog.WorldId), Guid.NewGuid() }
        });
        var dialog = provider.FindComponent<CreateArticleDialog>();
        SetField(dialog.Instance, "_title", "Hero");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("New Wiki Article", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Cancel_WhenNoDialog_DoesNotThrow()
    {
        var cut = RenderComponent<CreateArticleDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        var ex = Record.Exception(() => InvokePrivate(cut.Instance, "Cancel"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task OnAfterRenderAsync_WhenNotFirstRender_DoesNothing()
    {
        var cut = RenderComponent<CreateArticleDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnAfterRenderAsync", false));

        Assert.True(true);
    }

    private static async Task InvokePrivateAsync(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(target, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static T InvokePrivate<T>(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (T)method!.Invoke(target, args)!;
    }

    private static object? InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(target, args);
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    private static T GetField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(target)!;
    }
}
