using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Articles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

[ExcludeFromCodeCoverage]
public class ArticleHeaderTests : MudBlazorTestContext
{
    public ArticleHeaderTests()
    {
        _ = RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void ArticleHeader_RendersTitleAndIconPicker()
    {
        var cut = RenderComponent<ArticleHeader>(parameters => parameters
            .Add(p => p.Title, "Test Title"));

        Assert.Contains("Test Title", cut.Markup);
        Assert.NotNull(cut.FindComponent<IconPickerButton>());
    }

    [Fact]
    public async Task ArticleHeader_MetadataButton_TriggersCallback()
    {
        var clicked = false;
        var cut = RenderComponent<ArticleHeader>(parameters => parameters
            .Add(p => p.OnMetadataToggle, () => clicked = true));

        var metadataButton = cut.FindComponents<MudIconButton>()
            .First(b => b.Instance.Icon == Icons.Material.Filled.ChromeReaderMode);
        await metadataButton.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.True(clicked);
    }

    [Fact]
    public async Task ArticleHeader_TitleChange_TriggersCallbacks()
    {
        var changedTitle = string.Empty;
        var edited = false;
        var cut = RenderComponent<ArticleHeader>(parameters => parameters
            .Add(p => p.Title, "Old")
            .Add(p => p.TitleChanged, (string v) => changedTitle = v)
            .Add(p => p.OnTitleEdited, () => edited = true));

        await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "New Title" });

        Assert.Equal("New Title", changedTitle);
        Assert.True(edited);
    }

    [Fact]
    public async Task ArticleHeader_EnterKey_TriggersCallback()
    {
        var enterPressed = false;
        var cut = RenderComponent<ArticleHeader>(parameters => parameters
            .Add(p => p.OnEnterPressed, () => enterPressed = true));

        await cut.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.True(enterPressed);
    }

    [Fact]
    public async Task ArticleHeader_IconChanged_TriggersCallback()
    {
        string? icon = null;
        var cut = RenderComponent<ArticleHeader>(parameters => parameters
            .Add(p => p.OnIconChanged, (string? v) => icon = v));

        var picker = cut.FindComponent<IconPickerButton>();
        await cut.InvokeAsync(() => picker.Instance.OnIconChanged.InvokeAsync("fa-solid fa-dragon"));

        Assert.Equal("fa-solid fa-dragon", icon);
    }
}
