using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Articles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Components;

[ExcludeFromCodeCoverage]
public class EmojiPickerButtonTests : MudBlazorTestContext
{
    public EmojiPickerButtonTests()
    {
        Services.AddSingleton<ILogger<EmojiPickerButton>>(NullLogger<EmojiPickerButton>.Instance);
    }

    [Fact]
    public void EmojiPickerButton_WithoutEmoji_ShowsPlaceholder()
    {
        var cut = RenderComponent<EmojiPickerButton>();

        Assert.Contains("Add icon", cut.Markup);
        Assert.DoesNotContain("chronicis-emoji-clear-button", cut.Markup);
    }

    [Fact]
    public void EmojiPickerButton_WithEmoji_ShowsEmojiAndClearButton()
    {
        var cut = RenderComponent<EmojiPickerButton>(parameters => parameters
            .Add(p => p.CurrentEmoji, "😀"));

        Assert.Contains("😀", cut.Markup);
        Assert.Contains("chronicis-emoji-clear-button", cut.Markup);
    }

    [Fact]
    public async Task EmojiPickerButton_Clear_InvokesCallbackWithNull()
    {
        string? changedValue = "x";
        var cut = RenderComponent<EmojiPickerButton>(parameters => parameters
            .Add(p => p.CurrentEmoji, "😀")
            .Add(p => p.OnEmojiChanged, (string? emoji) => changedValue = emoji));

        await cut.Find(".chronicis-emoji-clear-button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Null(changedValue);
    }

    [Fact]
    public void EmojiPickerButton_ClickingButton_OpensPicker()
    {
        var cut = RenderComponent<EmojiPickerButton>();

        cut.Find(".chronicis-emoji-button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("chronicis-emoji-picker-dropdown", cut.Markup);
            Assert.Contains("chronicis-emoji-picker-backdrop", cut.Markup);
        });
    }

    [Fact]
    public async Task EmojiPickerButton_OnEmojiSelected_InvokesCallbackAndClosesPicker()
    {
        string? selected = null;
        var cut = RenderComponent<EmojiPickerButton>(parameters => parameters
            .Add(p => p.OnEmojiChanged, (string? emoji) => selected = emoji));

        cut.Find(".chronicis-emoji-button").Click();
        cut.WaitForAssertion(() => Assert.Contains("chronicis-emoji-picker-dropdown", cut.Markup));

        await cut.InvokeAsync(() => cut.Instance.OnEmojiSelected("🔥"));

        Assert.Equal("🔥", selected);
        cut.WaitForAssertion(() => Assert.DoesNotContain("chronicis-emoji-picker-dropdown", cut.Markup));
    }
}
