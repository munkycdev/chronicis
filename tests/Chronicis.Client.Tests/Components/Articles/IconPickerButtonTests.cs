using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Articles;
using Xunit;

namespace Chronicis.Client.Tests.Components;

[ExcludeFromCodeCoverage]
public class IconPickerButtonTests : MudBlazorTestContext
{
    [Fact]
    public void IconPickerButton_WithoutIcon_ShowsPlaceholder()
    {
        var cut = RenderComponent<IconPickerButton>();

        Assert.Contains("Add icon", cut.Markup);
        Assert.DoesNotContain("chronicis-icon-clear-button", cut.Markup);
    }

    [Fact]
    public void IconPickerButton_WithIcon_ShowsClearButton()
    {
        var cut = RenderComponent<IconPickerButton>(parameters => parameters
            .Add(p => p.CurrentIcon, "fa-solid fa-dragon"));

        Assert.Contains("fa-solid fa-dragon", cut.Markup);
        Assert.Contains("chronicis-icon-clear-button", cut.Markup);
    }

    [Fact]
    public void IconPickerButton_ClickingButton_OpensPicker()
    {
        var cut = RenderComponent<IconPickerButton>();

        cut.Find(".chronicis-icon-button").Click();

        Assert.Contains("chronicis-icon-picker-dropdown", cut.Markup);
        Assert.Contains("All Icons", cut.Markup);
    }

    [Fact]
    public async Task IconPickerButton_Clear_InvokesCallbackWithNull()
    {
        string? selected = "x";
        var cut = RenderComponent<IconPickerButton>(parameters => parameters
            .Add(p => p.CurrentIcon, "fa-solid fa-dragon")
            .Add(p => p.OnIconChanged, (string? icon) => selected = icon));

        await cut.Find(".chronicis-icon-clear-button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Null(selected);
    }

    [Fact]
    public void IconPickerButton_SearchNoResults_ShowsEmptyState()
    {
        var cut = RenderComponent<IconPickerButton>();
        cut.Find(".chronicis-icon-button").Click();

        cut.Find(".chronicis-icon-search-input").Input("zzzzzzzzz-no-match");

        Assert.Contains("No icons found", cut.Markup);
    }

    [Fact]
    public async Task IconPickerButton_ClickingIcon_InvokesCallbackAndCloses()
    {
        string? selected = null;
        var cut = RenderComponent<IconPickerButton>(parameters => parameters
            .Add(p => p.OnIconChanged, (string? icon) => selected = icon));

        cut.Find(".chronicis-icon-button").Click();
        var firstIcon = cut.Find(".icon-option");
        var classValue = firstIcon.GetAttribute("title");

        await firstIcon.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.NotNull(selected);
        Assert.DoesNotContain("chronicis-icon-picker-dropdown", cut.Markup);
        Assert.NotNull(classValue);
    }
}
