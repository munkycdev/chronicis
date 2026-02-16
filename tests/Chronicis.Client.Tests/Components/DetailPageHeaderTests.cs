using Bunit;
using Chronicis.Client.Components.Shared;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the DetailPageHeader component.
/// This component displays a header with breadcrumbs, icon, and editable title.
/// </summary>
public class DetailPageHeaderTests : MudBlazorTestContext
{
    [Fact]
    public void DetailPageHeader_WithTitle_RendersTitle()
    {
        // Arrange
        var title = "My World";

        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, title));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.Equal(title, textField.Instance.Value);
    }

    [Fact]
    public void DetailPageHeader_WithDefaultIcon_ShowsDescriptionIcon()
    {
        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var icon = cut.FindComponent<MudIcon>();
        Assert.Equal(Icons.Material.Filled.Description, icon.Instance.Icon);
    }

    [Fact]
    public void DetailPageHeader_WithCustomIcon_ShowsCustomIcon()
    {
        // Arrange
        var customIcon = Icons.Material.Filled.Public;

        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Icon, customIcon));

        // Assert
        var icon = cut.FindComponent<MudIcon>();
        Assert.Equal(customIcon, icon.Instance.Icon);
    }

    [Fact]
    public void DetailPageHeader_IconHasLargeSize()
    {
        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var icon = cut.FindComponent<MudIcon>();
        Assert.Equal(Size.Large, icon.Instance.Size);
    }

    [Fact]
    public void DetailPageHeader_WithPlaceholder_ShowsPlaceholder()
    {
        // Arrange
        var placeholder = "Enter world name";

        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Placeholder, placeholder));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.Equal(placeholder, textField.Instance.Placeholder);
    }

    [Fact]
    public void DetailPageHeader_WithDefaultPlaceholder_ShowsName()
    {
        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.Equal("Name", textField.Instance.Placeholder);
    }

    [Fact]
    public void DetailPageHeader_TitleField_HasTextVariant()
    {
        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.Equal(Variant.Text, textField.Instance.Variant);
        Assert.False(textField.Instance.Underline);
    }

    [Fact]
    public void DetailPageHeader_TitleField_IsImmediate()
    {
        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.True(textField.Instance.Immediate);
    }

    [Fact]
    public void DetailPageHeader_HasRuneDivider()
    {
        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var divider = cut.Find(".chronicis-rune-divider");
        Assert.NotNull(divider);
    }

    [Fact]
    public void DetailPageHeader_WithBreadcrumbs_RendersBreadcrumbs()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new("Worlds", "/worlds"),
            new("My World", null, disabled: true)
        };

        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Breadcrumbs, breadcrumbs));

        // Assert
        var breadcrumbComponent = cut.FindComponent<ChroniclsBreadcrumbs>();
        Assert.NotNull(breadcrumbComponent);
    }

    [Fact]
    public void DetailPageHeader_TitleChanged_TriggersCallback()
    {
        // Arrange
        var newTitle = "Updated Title";
        var callbackTitle = string.Empty;

        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Original")
            .Add(p => p.TitleChanged, title => callbackTitle = title));

        var textField = cut.FindComponent<MudTextField<string>>();
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
        textField.Instance.Value = newTitle;
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.
        cut.InvokeAsync(async () => await textField.Instance.ValueChanged.InvokeAsync(newTitle));

        // Assert
        Assert.Equal(newTitle, callbackTitle);
    }

    [Fact]
    public void DetailPageHeader_TitleEdited_TriggersCallback()
    {
        // Arrange
        var editedCallbackInvoked = false;

        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.OnTitleEdited, () => editedCallbackInvoked = true));

        var textField = cut.FindComponent<MudTextField<string>>();
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
        textField.Instance.Value = "New Title";
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.
        cut.InvokeAsync(async () => await textField.Instance.ValueChanged.InvokeAsync("New Title"));

        // Assert
        Assert.True(editedCallbackInvoked);
    }

    [Theory]
    [InlineData("My World", Icons.Material.Filled.Public)]
    [InlineData("Campaign 1", Icons.Material.Filled.Campaign)]
    [InlineData("Arc Alpha", Icons.Material.Filled.AccountTree)]
    public void DetailPageHeader_RendersVariousTitleIconCombinations(string title, string icon)
    {
        // Act
        var cut = RenderComponent<DetailPageHeader>(parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.Icon, icon));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.Equal(title, textField.Instance.Value);

        var mudIcon = cut.FindComponent<MudIcon>();
        Assert.Equal(icon, mudIcon.Instance.Icon);
    }
}
