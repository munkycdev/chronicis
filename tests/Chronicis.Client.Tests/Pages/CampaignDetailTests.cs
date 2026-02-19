using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class CampaignDetailTests : MudBlazorTestContext
{
    [Fact]
    public void CampaignDetail_OnNameChanged_SetsUnsavedChanges()
    {
        var sut = CreateRenderedSut().Instance;

        InvokePrivate(sut, "OnNameChanged");

        Assert.True(GetPrivateBoolField(sut, "_hasUnsavedChanges"));
    }

    [Fact]
    public void CampaignDetail_OnDescriptionChanged_SetsUnsavedChanges()
    {
        var sut = CreateRenderedSut().Instance;

        InvokePrivate(sut, "OnDescriptionChanged");

        Assert.True(GetPrivateBoolField(sut, "_hasUnsavedChanges"));
    }

    [Fact]
    public async Task CampaignDetail_SaveCampaign_WhenCampaignNull_DoesNotCallUpdate()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveCampaign");

        await rendered.CampaignApi.DidNotReceive().UpdateCampaignAsync(Arg.Any<Guid>(), Arg.Any<CampaignUpdateDto>());
    }

    [Fact]
    public async Task CampaignDetail_SaveCampaign_WhenAlreadySaving_DoesNotCallUpdate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDetailDto { Id = rendered.CampaignId, Name = "Original" });
        SetPrivateField(rendered.Instance, "_isSaving", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveCampaign");

        await rendered.CampaignApi.DidNotReceive().UpdateCampaignAsync(Arg.Any<Guid>(), Arg.Any<CampaignUpdateDto>());
    }

    [Fact]
    public async Task CampaignDetail_SaveCampaign_WhenUpdateSucceeds_RefreshesTreeAndClearsDirtyFlag()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDetailDto { Id = rendered.CampaignId, Name = "Original", Description = "Old" });
        SetPrivateField(rendered.Instance, "_editName", " Updated Name ");
        SetPrivateField(rendered.Instance, "_editDescription", " Updated Description ");
        SetPrivateField(rendered.Instance, "_hasUnsavedChanges", true);

        rendered.CampaignApi.UpdateCampaignAsync(rendered.CampaignId, Arg.Any<CampaignUpdateDto>())
            .Returns(new CampaignDto { Id = rendered.CampaignId, Name = "Updated Name", Description = "Updated Description" });

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveCampaign");

        await rendered.CampaignApi.Received(1).UpdateCampaignAsync(rendered.CampaignId,
            Arg.Is<CampaignUpdateDto>(d => d.Name == "Updated Name" && d.Description == "Updated Description"));
        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.False(GetPrivateBoolField(rendered.Instance, "_hasUnsavedChanges"));
        Assert.False(GetPrivateBoolField(rendered.Instance, "_isSaving"));
    }

    [Fact]
    public async Task CampaignDetail_OnActiveToggle_WhenCampaignNull_ReturnsEarly()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        await rendered.CampaignApi.DidNotReceive().ActivateCampaignAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CampaignDetail_OnActiveToggle_WhenAlreadyToggling_ReturnsEarly()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDetailDto { Id = rendered.CampaignId, Name = "Test" });
        SetPrivateField(rendered.Instance, "_isTogglingActive", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        await rendered.CampaignApi.DidNotReceive().ActivateCampaignAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CampaignDetail_OnActiveToggle_ActivateSuccess_SetsCampaignActive()
    {
        var rendered = CreateRenderedSut();
        var campaign = new CampaignDetailDto { Id = rendered.CampaignId, Name = "Test", IsActive = false };
        SetPrivateField(rendered.Instance, "_campaign", campaign);

        rendered.CampaignApi.ActivateCampaignAsync(rendered.CampaignId).Returns(true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        Assert.True(campaign.IsActive);
        Assert.False(GetPrivateBoolField(rendered.Instance, "_isTogglingActive"));
    }

    [Fact]
    public async Task CampaignDetail_OnActiveToggle_ActivateFailure_DoesNotSetCampaignActive()
    {
        var rendered = CreateRenderedSut();
        var campaign = new CampaignDetailDto { Id = rendered.CampaignId, Name = "Test", IsActive = false };
        SetPrivateField(rendered.Instance, "_campaign", campaign);

        rendered.CampaignApi.ActivateCampaignAsync(rendered.CampaignId).Returns(false);

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        Assert.False(campaign.IsActive);
        Assert.False(GetPrivateBoolField(rendered.Instance, "_isTogglingActive"));
    }

    [Fact]
    public async Task CampaignDetail_OnActiveToggle_DeactivatePath_DoesNotCallActivateApi()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDetailDto { Id = rendered.CampaignId, Name = "Test", IsActive = true });

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", false);

        await rendered.CampaignApi.DidNotReceive().ActivateCampaignAsync(Arg.Any<Guid>());
        Assert.False(GetPrivateBoolField(rendered.Instance, "_isTogglingActive"));
    }

    [Fact]
    public async Task CampaignDetail_NavigateToArc_NavigatesToArcRoute()
    {
        var rendered = CreateRenderedSut();
        var arcId = Guid.NewGuid();

        await InvokePrivateAsync(rendered.Instance, "NavigateToArc", arcId);

        Assert.EndsWith($"/arc/{arcId}", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CampaignDetail_LoadCampaignAsync_WhenCampaignNull_NavigatesToDashboard()
    {
        var rendered = CreateRenderedSut();
        rendered.CampaignApi.GetCampaignAsync(rendered.CampaignId).Returns((CampaignDetailDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadCampaignAsync");

        Assert.EndsWith("/dashboard", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CampaignDetail_LoadCampaignAsync_WhenWorldMissing_UsesFallbackBreadcrumbs()
    {
        var rendered = CreateRenderedSut();
        rendered.CampaignApi.GetCampaignAsync(rendered.CampaignId).Returns(new CampaignDetailDto
        {
            Id = rendered.CampaignId,
            WorldId = Guid.NewGuid(),
            Name = "Fallback Campaign",
            CreatedAt = new DateTime(2025, 1, 1)
        });
        rendered.ArcApi.GetArcsByCampaignAsync(rendered.CampaignId).Returns(new List<ArcDto>());
        rendered.WorldApi.GetWorldAsync(Arg.Any<Guid>()).Returns((WorldDetailDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadCampaignAsync");
        rendered.Cut.Render();

        var breadcrumbs = GetPrivateField<List<BreadcrumbItem>>(rendered.Instance, "_breadcrumbs");
        Assert.Equal(2, breadcrumbs.Count);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);
        Assert.Equal("Fallback Campaign", breadcrumbs[1].Text);
        rendered.BreadcrumbService.DidNotReceiveWithAnyArgs().ForCampaign(default!, default!);
        Assert.Empty(GetPrivateField<List<ArcDto>>(rendered.Instance, "_arcs"));
    }

    [Fact]
    public async Task CampaignDetail_LoadCampaignAsync_WhenWorldAndArcsPresent_UsesBreadcrumbServiceAndRendersStarted()
    {
        var rendered = CreateRenderedSut();
        var worldId = Guid.NewGuid();
        var startedAt = new DateTime(2024, 2, 3);
        rendered.CampaignApi.GetCampaignAsync(rendered.CampaignId).Returns(new CampaignDetailDto
        {
            Id = rendered.CampaignId,
            WorldId = worldId,
            Name = "Storm King",
            CreatedAt = new DateTime(2024, 1, 1),
            StartedAt = startedAt
        });
        rendered.ArcApi.GetArcsByCampaignAsync(rendered.CampaignId).Returns(new List<ArcDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Act I", SortOrder = 1, SessionCount = 3 }
        });
        rendered.WorldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId, Name = "Faerun", Slug = "faerun" });
        var breadcrumbItems = new List<BreadcrumbItem>
        {
            new("Dashboard", "/dashboard"),
            new("Faerun", "/world/faerun"),
            new("Storm King", null, true)
        };
        rendered.BreadcrumbService.ForCampaign(Arg.Any<CampaignDetailDto>(), Arg.Any<WorldDto>()).Returns(breadcrumbItems);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadCampaignAsync");
        rendered.Cut.Render();

        rendered.BreadcrumbService.Received(2).ForCampaign(
            Arg.Is<CampaignDetailDto>(c => c.Id == rendered.CampaignId),
            Arg.Is<WorldDto>(w => w.Id == worldId));
        Assert.Single(GetPrivateField<List<ArcDto>>(rendered.Instance, "_arcs"));
        Assert.Contains(startedAt.ToString("MMMM d, yyyy"), rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CampaignDetail_SaveCampaign_WhenUpdateReturnsNull_KeepsUnsavedChanges()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDetailDto { Id = rendered.CampaignId, Name = "Original" });
        SetPrivateField(rendered.Instance, "_editName", "Updated");
        SetPrivateField(rendered.Instance, "_hasUnsavedChanges", true);
        rendered.CampaignApi.UpdateCampaignAsync(rendered.CampaignId, Arg.Any<CampaignUpdateDto>()).Returns((CampaignDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveCampaign");

        Assert.True(GetPrivateBoolField(rendered.Instance, "_hasUnsavedChanges"));
        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task CampaignDetail_SaveCampaign_WhenUpdateThrows_ResetsSavingFlag()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDetailDto { Id = rendered.CampaignId, Name = "Original" });
        SetPrivateField(rendered.Instance, "_editName", "Updated");
        rendered.CampaignApi.UpdateCampaignAsync(rendered.CampaignId, Arg.Any<CampaignUpdateDto>())
            .Returns(Task.FromException<CampaignDto?>(new Exception("boom")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveCampaign");

        Assert.False(GetPrivateBoolField(rendered.Instance, "_isSaving"));
    }

    [Fact]
    public async Task CampaignDetail_OnActiveToggle_WhenActivateThrows_ResetsTogglingFlag()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDetailDto { Id = rendered.CampaignId, Name = "Test" });
        rendered.CampaignApi.ActivateCampaignAsync(rendered.CampaignId)
            .Returns(Task.FromException<bool>(new Exception("boom")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        Assert.False(GetPrivateBoolField(rendered.Instance, "_isTogglingActive"));
    }

    [Fact]
    public async Task CampaignDetail_CreateArc_WhenDialogCanceled_DoesNotRefreshTree()
    {
        var rendered = CreateRenderedSut();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        rendered.DialogService.ShowAsync<CreateArcDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateArc");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task CampaignDetail_CreateArc_WhenDialogReturnsArc_NavigatesToArc()
    {
        var rendered = CreateRenderedSut();
        var worldId = Guid.NewGuid();
        rendered.CampaignApi.GetCampaignAsync(rendered.CampaignId).Returns(new CampaignDetailDto
        {
            Id = rendered.CampaignId,
            WorldId = worldId,
            Name = "Campaign",
            CreatedAt = new DateTime(2024, 1, 1)
        });
        rendered.ArcApi.GetArcsByCampaignAsync(rendered.CampaignId).Returns(new List<ArcDto>());
        rendered.WorldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId, Name = "World", Slug = "world" });

        var createdArc = new ArcDto { Id = Guid.NewGuid(), Name = "Arc" };
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(createdArc)));
        rendered.DialogService.ShowAsync<CreateArcDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateArc");

        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.EndsWith($"/arc/{createdArc.Id}", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    private RenderedContext CreateRenderedSut()
    {
        var campaignApi = Substitute.For<ICampaignApiService>();
        var arcApi = Substitute.For<IArcApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var snackbar = Substitute.For<ISnackbar>();
        var dialogService = Substitute.For<IDialogService>();
        var jsRuntime = Substitute.For<IJSRuntime>();
        var summaryApi = Substitute.For<IAISummaryApiService>();

        Services.AddSingleton(campaignApi);
        Services.AddSingleton(arcApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton(treeState);
        Services.AddSingleton(breadcrumbService);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(dialogService);
        Services.AddSingleton(jsRuntime);
        Services.AddSingleton(summaryApi);

        summaryApi.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        summaryApi.GetEntitySummaryAsync(Arg.Any<string>(), Arg.Any<Guid>()).Returns((EntitySummaryDto?)null);

        var campaignId = Guid.NewGuid();
        campaignApi.GetCampaignAsync(campaignId).Returns((CampaignDetailDto?)null);
        arcApi.GetArcsByCampaignAsync(campaignId).Returns(new List<ArcDto>());
        var cut = RenderComponent<CampaignDetail>(parameters => parameters.Add(p => p.CampaignId, campaignId));
        var navigation = Services.GetRequiredService<NavigationManager>();

        return new RenderedContext(cut, cut.Instance, campaignApi, arcApi, worldApi, breadcrumbService, dialogService, treeState, navigation, campaignId);
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(instance, args);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var result = method!.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<CampaignDetail> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }

    private static bool GetPrivateBoolField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (bool)field!.GetValue(instance)!;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private sealed record RenderedContext(
        IRenderedComponent<CampaignDetail> Cut,
        CampaignDetail Instance,
        ICampaignApiService CampaignApi,
        IArcApiService ArcApi,
        IWorldApiService WorldApi,
        IBreadcrumbService BreadcrumbService,
        IDialogService DialogService,
        ITreeStateService TreeState,
        NavigationManager Navigation,
        Guid CampaignId);
}
