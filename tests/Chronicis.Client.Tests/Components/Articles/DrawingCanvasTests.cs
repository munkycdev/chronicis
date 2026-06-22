using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Articles;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components.Articles;

/// <summary>
/// Unit tests for DrawingCanvas component logic.
/// Tests button disabled states, JS interop invocations, stroke count changes, and disposal.
/// </summary>
[ExcludeFromCodeCoverage]
public class DrawingCanvasTests : MudBlazorTestContext
{
    #region Save Button Disabled State

    [Fact]
    public void SaveButton_WhenNoStrokes_IsDisabled()
    {
        var cut = RenderComponent<DrawingCanvas>();

        var saveButton = FindButtonByText(cut, "Save");
        Assert.True(saveButton.Instance.Disabled);
    }

    [Fact]
    public void SaveButton_WhenIsSavingTrue_IsDisabled()
    {
        var cut = RenderComponent<DrawingCanvas>(p => p
            .Add(x => x.IsSaving, true));

        SimulateStrokeCountChanged(cut, 1);

        var saveButton = FindButtonByText(cut, "Save");
        Assert.True(saveButton.Instance.Disabled);
    }

    [Fact]
    public void SaveButton_WhenHasStrokesAndNotSaving_IsEnabled()
    {
        var cut = RenderComponent<DrawingCanvas>();

        SimulateStrokeCountChanged(cut, 1);

        var saveButton = FindButtonByText(cut, "Save");
        Assert.False(saveButton.Instance.Disabled);
    }

    #endregion

    #region Transcribe Button Disabled State

    [Fact]
    public void TranscribeButton_WhenNoStrokes_IsDisabled()
    {
        var cut = RenderComponent<DrawingCanvas>();

        var transcribeButton = FindButtonByText(cut, "Transcribe");
        Assert.True(transcribeButton.Instance.Disabled);
    }

    [Fact]
    public void TranscribeButton_WhenIsSavingTrue_IsDisabled()
    {
        var cut = RenderComponent<DrawingCanvas>(p => p
            .Add(x => x.IsSaving, true));

        SimulateStrokeCountChanged(cut, 1);

        var transcribeButton = FindButtonByText(cut, "Transcribe");
        Assert.True(transcribeButton.Instance.Disabled);
    }

    [Fact]
    public void TranscribeButton_WhenHasStrokesAndNotSaving_IsEnabled()
    {
        var cut = RenderComponent<DrawingCanvas>();

        SimulateStrokeCountChanged(cut, 1);

        var transcribeButton = FindButtonByText(cut, "Transcribe");
        Assert.False(transcribeButton.Instance.Disabled);
    }

    #endregion

    #region JS Interop Calls

    [Fact]
    public void OnAfterRender_CallsDrawingCanvasInitialize()
    {
        RenderComponent<DrawingCanvas>();

        var initCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasInitialize");
        Assert.Single(initCalls);
    }

    [Fact]
    public async Task SelectColor_WhenInitialized_CallsDrawingCanvasSetColor()
    {
        SetupInitializeReturnsTrue();
        var cut = RenderComponent<DrawingCanvas>();

        // Click the red color button
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "SelectColor", "#FF0000"));

        var setColorCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasSetColor");
        Assert.Single(setColorCalls);
        Assert.Equal("#FF0000", setColorCalls.First().Arguments[1]?.ToString());
    }

    [Fact]
    public async Task SelectTool_WhenInitialized_CallsDrawingCanvasSetTool()
    {
        SetupInitializeReturnsTrue();
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "SelectTool", "eraser"));

        var setToolCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasSetTool");
        Assert.Single(setToolCalls);
        Assert.Equal("eraser", setToolCalls.First().Arguments[1]?.ToString());
    }

    [Fact]
    public async Task Undo_WhenInitialized_CallsDrawingCanvasUndo()
    {
        SetupInitializeReturnsTrue();
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "Undo"));

        var undoCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasUndo");
        Assert.Single(undoCalls);
    }

    [Fact]
    public async Task Redo_WhenInitialized_CallsDrawingCanvasRedo()
    {
        SetupInitializeReturnsTrue();
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "Redo"));

        var redoCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasRedo");
        Assert.Single(redoCalls);
    }

    [Fact]
    public async Task HandleSave_WhenInitializedWithStrokes_CallsExportPngAndInvokesOnSave()
    {
        SetupInitializeReturnsTrue();
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        SetupExportPngReturns(pngBytes);

        byte[]? receivedBytes = null;
        var cut = RenderComponent<DrawingCanvas>(p => p
            .Add(x => x.OnSave, EventCallback.Factory.Create<byte[]>(this, b => receivedBytes = b)));

        SimulateStrokeCountChanged(cut, 1);
        SetInitializedField(cut.Instance, true);

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleSave"));

        var exportCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasExportPng");
        Assert.Single(exportCalls);
        Assert.Equal(pngBytes, receivedBytes);
    }

    [Fact]
    public async Task HandleSave_WhenNotInitialized_DoesNotCallExportPng()
    {
        var cut = RenderComponent<DrawingCanvas>();

        SimulateStrokeCountChanged(cut, 1);

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleSave"));

        var exportCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasExportPng");
        Assert.Empty(exportCalls);
    }

    [Fact]
    public async Task HandleSave_WhenNoStrokes_DoesNotCallExportPng()
    {
        SetupInitializeReturnsTrue();
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleSave"));

        var exportCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasExportPng");
        Assert.Empty(exportCalls);
    }

    [Fact]
    public async Task HandleTranscribe_WhenInitializedWithStrokes_CallsExportPngAndInvokesOnTranscribe()
    {
        SetupInitializeReturnsTrue();
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        SetupExportPngReturns(pngBytes);

        byte[]? receivedBytes = null;
        var cut = RenderComponent<DrawingCanvas>(p => p
            .Add(x => x.OnTranscribe, EventCallback.Factory.Create<byte[]>(this, b => receivedBytes = b)));

        SimulateStrokeCountChanged(cut, 1);
        SetInitializedField(cut.Instance, true);

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleTranscribe"));

        var exportCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasExportPng");
        Assert.Single(exportCalls);
        Assert.Equal(pngBytes, receivedBytes);
    }

    [Fact]
    public async Task HandleTranscribe_WhenNotInitialized_DoesNotCallExportPng()
    {
        var cut = RenderComponent<DrawingCanvas>();

        SimulateStrokeCountChanged(cut, 1);

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleTranscribe"));

        var exportCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasExportPng");
        Assert.Empty(exportCalls);
    }

    [Fact]
    public async Task HandleTranscribe_WhenNoStrokes_DoesNotCallExportPng()
    {
        SetupInitializeReturnsTrue();
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleTranscribe"));

        var exportCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasExportPng");
        Assert.Empty(exportCalls);
    }

    [Fact]
    public async Task HandleSave_WhenExportReturnsNull_DoesNotInvokeOnSave()
    {
        SetupInitializeReturnsTrue();
        SetupExportPngReturns(null!);

        var saveCalled = false;
        var cut = RenderComponent<DrawingCanvas>(p => p
            .Add(x => x.OnSave, EventCallback.Factory.Create<byte[]>(this, _ => saveCalled = true)));

        SimulateStrokeCountChanged(cut, 1);
        SetInitializedField(cut.Instance, true);

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleSave"));

        Assert.False(saveCalled);
    }

    [Fact]
    public async Task HandleTranscribe_WhenExportReturnsNull_DoesNotInvokeOnTranscribe()
    {
        SetupInitializeReturnsTrue();
        SetupExportPngReturns(null!);

        var transcribeCalled = false;
        var cut = RenderComponent<DrawingCanvas>(p => p
            .Add(x => x.OnTranscribe, EventCallback.Factory.Create<byte[]>(this, _ => transcribeCalled = true)));

        SimulateStrokeCountChanged(cut, 1);
        SetInitializedField(cut.Instance, true);

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleTranscribe"));

        Assert.False(transcribeCalled);
    }

    #endregion

    #region SelectColor / SelectTool When Not Initialized

    [Fact]
    public async Task SelectColor_WhenNotInitialized_DoesNotCallJS()
    {
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "SelectColor", "#FF0000"));

        var setColorCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasSetColor");
        Assert.Empty(setColorCalls);
    }

    [Fact]
    public async Task SelectTool_WhenNotInitialized_DoesNotCallJS()
    {
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "SelectTool", "eraser"));

        var setToolCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasSetTool");
        Assert.Empty(setToolCalls);
    }

    [Fact]
    public async Task Undo_WhenNotInitialized_DoesNotCallJS()
    {
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "Undo"));

        var undoCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasUndo");
        Assert.Empty(undoCalls);
    }

    [Fact]
    public async Task Redo_WhenNotInitialized_DoesNotCallJS()
    {
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "Redo"));

        var redoCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasRedo");
        Assert.Empty(redoCalls);
    }

    #endregion

    #region OnStrokeCountChanged

    [Fact]
    public void OnStrokeCountChanged_UpdatesStrokeCountAndReEnablesButtons()
    {
        var cut = RenderComponent<DrawingCanvas>();

        // Initially disabled
        var saveButton = FindButtonByText(cut, "Save");
        Assert.True(saveButton.Instance.Disabled);

        // Simulate JS calling OnStrokeCountChanged
        cut.InvokeAsync(() => cut.Instance.OnStrokeCountChanged(3));

        saveButton = FindButtonByText(cut, "Save");
        Assert.False(saveButton.Instance.Disabled);
    }

    #endregion

    #region DisposeAsync

    [Fact]
    public async Task DisposeAsync_WhenInitialized_CallsDrawingCanvasDispose()
    {
        SetupInitializeReturnsTrue();
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(async () => await ((IAsyncDisposable)cut.Instance).DisposeAsync());

        var disposeCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasDispose");
        Assert.Single(disposeCalls);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotInitialized_DoesNotCallDrawingCanvasDispose()
    {
        var cut = RenderComponent<DrawingCanvas>();

        await cut.InvokeAsync(async () => await ((IAsyncDisposable)cut.Instance).DisposeAsync());

        var disposeCalls = JSInterop.Invocations
            .Where(i => i.Identifier == "drawingCanvasDispose");
        Assert.Empty(disposeCalls);
    }

    [Fact]
    public async Task DisposeAsync_WhenJSDisconnected_DoesNotThrow()
    {
        SetupInitializeReturnsTrue();
        JSInterop.SetupVoid("drawingCanvasDispose", _ => true)
            .SetException(new JSDisconnectedException("gone"));

        var cut = RenderComponent<DrawingCanvas>();

        var exception = await Record.ExceptionAsync(async () =>
            await cut.InvokeAsync(async () => await ((IAsyncDisposable)cut.Instance).DisposeAsync()));

        Assert.Null(exception);
    }

    #endregion

    #region GetColorButtonStyle

    [Fact]
    public void ColorButtons_DefaultColor_IsBlack()
    {
        var cut = RenderComponent<DrawingCanvas>();

        // The first color button (black) should have the primary border style
        var style = InvokePrivateMethod<string>(cut.Instance, "GetColorButtonStyle", "#000000");
        Assert.Contains("3px solid", style);
    }

    [Fact]
    public void ColorButtons_NonSelectedColor_HasTransparentBorder()
    {
        var cut = RenderComponent<DrawingCanvas>();

        var style = InvokePrivateMethod<string>(cut.Instance, "GetColorButtonStyle", "#FF0000");
        Assert.Contains("2px solid transparent", style);
    }

    #endregion

    #region Helpers

    private void SetupInitializeReturnsTrue()
    {
        JSInterop.Setup<bool>("drawingCanvasInitialize", _ => true).SetResult(true);
    }

    private void SetupExportPngReturns(byte[] result)
    {
        JSInterop.Setup<byte[]>("drawingCanvasExportPng", _ => true).SetResult(result);
    }

    private static void SimulateStrokeCountChanged(IRenderedComponent<DrawingCanvas> cut, int count)
    {
        cut.InvokeAsync(() => cut.Instance.OnStrokeCountChanged(count));
    }

    private static void SetInitializedField(DrawingCanvas instance, bool value)
    {
        var field = typeof(DrawingCanvas).GetField("_initialized", BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(instance, value);
    }

    private static IRenderedComponent<MudButton> FindButtonByText(IRenderedComponent<DrawingCanvas> cut, string text)
    {
        var buttons = cut.FindComponents<MudButton>();
        return buttons.First(b => b.Markup.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task InvokePrivateTask(object instance, string methodName, params object?[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        var result = method!.Invoke(instance, args);
        if (result is Task task)
            await task;
    }

    private static T InvokePrivateMethod<T>(object instance, string methodName, params object?[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)method!.Invoke(instance, args)!;
    }

    #endregion
}
