using Chronicis.CaptureApp.UI;
using Chronicis.CaptureApp.Utilities;
using System;
using System.Windows.Forms;

namespace Chronicis.CaptureApp.Services;

public class SystemTrayService : ISystemTrayService, IDisposable
{
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _contextMenu;
    private Form? _mainForm;
    private bool _isRecording;
    private bool _hasShownMinimizeNotification;

    public void Initialize(Form mainForm)
    {
        _mainForm = mainForm;

        // Create context menu
        _contextMenu = new ContextMenuStrip();

        var startItem = new ToolStripMenuItem("Start Recording", null, OnStartRecording);
        var stopItem = new ToolStripMenuItem("Stop Recording", null, OnStopRecording) { Enabled = false };
        var showHideItem = new ToolStripMenuItem("Show/Hide Window", null, OnShowHideWindow);
        var exitItem = new ToolStripMenuItem("Exit", null, OnExit);

        startItem.Name = "StartRecording";
        stopItem.Name = "StopRecording";

        _contextMenu.Items.Add(startItem);
        _contextMenu.Items.Add(stopItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(showHideItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(exitItem);

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = IconHelper.CreateDragonIcon(false),
            Text = "Chronicis Audio Capture",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += OnShowHideWindow;

        // Hook into form close event to minimize instead
        _mainForm.FormClosing += OnFormClosing;
    }

    public void SetRecordingState(bool isRecording)
    {
        _isRecording = isRecording;

        if (_trayIcon != null)
        {
            _trayIcon.Icon = IconHelper.CreateDragonIcon(isRecording);
        }

        if (_contextMenu != null)
        {
            var startItem = _contextMenu.Items["StartRecording"] as ToolStripMenuItem;
            var stopItem = _contextMenu.Items["StopRecording"] as ToolStripMenuItem;

            if (startItem != null) startItem.Enabled = !isRecording;
            if (stopItem != null) stopItem.Enabled = isRecording;
        }
    }

    public void ShowBalloonTip(string title, string message)
    {
        _trayIcon?.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
    }

    private void OnStartRecording(object? sender, EventArgs e)
    {
        // MainForm will handle this via a public method
        if (_mainForm is MainForm form)
        {
            form.StartRecordingFromTray();
        }
    }

    private void OnStopRecording(object? sender, EventArgs e)
    {
        if (_mainForm is MainForm form)
        {
            form.StopRecordingFromTray();
        }
    }

    private void OnShowHideWindow(object? sender, EventArgs e)
    {
        if (_mainForm == null) return;

        if (_mainForm.Visible)
        {
            _mainForm.Hide();

            if (!_hasShownMinimizeNotification)
            {
                ShowBalloonTip("Chronicis", "Chronicis is still running in the background");
                _hasShownMinimizeNotification = true;
            }
        }
        else
        {
            _mainForm.Show();
            _mainForm.WindowState = FormWindowState.Normal;
            _mainForm.BringToFront();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            _mainForm?.Hide();

            if (!_hasShownMinimizeNotification)
            {
                ShowBalloonTip("Chronicis", "Chronicis is still running in the background");
                _hasShownMinimizeNotification = true;
            }
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        if (_isRecording)
        {
            var result = MessageBox.Show(
                "Recording is still in progress. Are you sure you want to exit?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;
        }

        _trayIcon!.Visible = false;
        Application.Exit();
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _contextMenu?.Dispose();
    }
}