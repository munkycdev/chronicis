using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Articles;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

[ExcludeFromCodeCoverage]
public class MarkdownToolbarTests : MudBlazorTestContext
{
    public MarkdownToolbarTests()
    {
        _ = RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public async Task MarkdownToolbar_BoldButton_InsertsBoldSyntax()
    {
        string inserted = string.Empty;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnInsertMarkdown, (string value) => inserted = value));

        var bold = cut.FindComponents<MudIconButton>()
            .First(b => b.Instance.Icon == Icons.Material.Filled.FormatBold);
        await bold.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Equal("**bold text**", inserted);
    }

    [Fact]
    public async Task MarkdownToolbar_TableButton_InsertsTableTemplate()
    {
        string inserted = string.Empty;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.OnInsertMarkdown, (string value) => inserted = value));

        var table = cut.FindComponents<MudIconButton>()
            .First(b => b.Instance.Icon == Icons.Material.Filled.TableChart);
        await table.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Contains("| Column 1 | Column 2 | Column 3 |", inserted);
    }

    [Fact]
    public async Task MarkdownToolbar_PreviewButton_InvokesToggleCallback()
    {
        var toggled = false;
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.IsPreviewMode, false)
            .Add(p => p.OnTogglePreview, () => toggled = true));

        var preview = cut.FindComponents<MudIconButton>()
            .First(b => b.Instance.Icon == Icons.Material.Filled.Preview);
        await preview.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.True(toggled);
    }

    [Fact]
    public void MarkdownToolbar_WhenPreviewMode_ShowsEditIconAndLabel()
    {
        var cut = RenderComponent<MarkdownToolbar>(parameters => parameters
            .Add(p => p.IsPreviewMode, true));

        Assert.Contains(Icons.Material.Filled.Edit, cut.Markup);
        Assert.DoesNotContain(Icons.Material.Filled.Preview, cut.Markup);
    }
}
