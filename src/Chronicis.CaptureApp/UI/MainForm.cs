using Chronicis.CaptureApp.Models;
using Chronicis.CaptureApp.Services;
using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Whisper.net.Ggml;

namespace Chronicis.CaptureApp.UI;

public class MainForm : Form
{
    // Services (injected)
    private readonly IAudioSourceProvider _audioSourceProvider;
    private readonly IAudioCaptureService _audioCaptureService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ISettingsService _settingsService;
    private readonly ISystemTrayService _systemTrayService;
    private readonly ISpeakerDetectionService _speakerDetectionService;

    // UI Controls
    private Guna2CheckBox _chkEnableSpeakerDetection = null!;
    private Guna2Button _btnEditSpeakers = null!;
    private Guna2ComboBox _cmbAudioSources = null!;
    private Guna2Button _btnStart = null!;
    private Guna2Button _btnStop = null!;
    private Guna2Button _btnRefresh = null!;
    private Label _lblStatus = null!;
    private Guna2TextBox _txtTranscript = null!;
    private Label _lblTranscriptTitle = null!;
    private Label _lblSourceLabel = null!;
    private Label _lblInfo = null!;
    private Guna2Panel _mainPanel = null!;
    private Guna2ShadowPanel _controlPanel = null!;
    private Guna2ShadowPanel _transcriptPanel = null!;
    private Label _lblHeader = null!;
    private Guna2ComboBox _cmbChunkSize = null!;
    private Label _lblChunkSize = null!;
    private Guna2ComboBox _cmbModel = null!;
    private Label _lblModel = null!;
    private Label _lblQueueStatus = null!;

    // State
    private AppSettings _settings = null!;
    private StringBuilder _fullTranscript = new();
    private List<AudioSource> _audioSources = new();
    private TranscriptWithSpeakers _transcriptWithSpeakers = new();

    public MainForm(
    IAudioSourceProvider audioSourceProvider,
    IAudioCaptureService audioCaptureService,
    ITranscriptionService transcriptionService,
    ISettingsService settingsService,
    ISystemTrayService systemTrayService,
    ISpeakerDetectionService speakerDetectionService)
    {
        _audioSourceProvider = audioSourceProvider;
        _audioCaptureService = audioCaptureService;
        _transcriptionService = transcriptionService;
        _settingsService = settingsService;
        _systemTrayService = systemTrayService;
        _speakerDetectionService = speakerDetectionService;

        InitializeComponent();
        InitializeServices();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form settings
        this.ClientSize = new Size(820, 750);
        this.Text = "Chronicis Audio Capture";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(240, 242, 245);
        this.Font = new Font("Segoe UI", 9F);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        CreateModernUI();

        this.ResumeLayout(false);
    }

    private void InitializeServices()
    {
        // Load settings
        _settings = _settingsService.Load();

        // Initialize system tray
        _systemTrayService.Initialize(this);

        // Load audio sources
        LoadAudioSources();

        // Restore saved settings
        RestoreSettings();

        // Hook up events
        _audioCaptureService.ChunkReady += OnChunkReady;
        _audioCaptureService.QueueStatsUpdated += OnQueueStatsUpdated;
        _audioCaptureService.RecordingStopped += OnRecordingStopped;

        // Initialize Whisper
        Task.Run(async () => await InitializeWhisperAsync());
    }

    private void CreateModernUI()
    {
        // Main container panel
        _mainPanel = new Guna2Panel
        {
            Dock = DockStyle.Fill,
            FillColor = Color.FromArgb(244, 240, 234),
            Padding = new Padding(20)
        };
        this.Controls.Add(_mainPanel);

        int yPos = 10;

        // Header
        _lblHeader = new Label
        {
            Text = "🐉 Chronicis Audio Capture",
            Location = new Point(10, yPos),
            AutoSize = true,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(196, 175, 142)
        };
        _mainPanel.Controls.Add(_lblHeader);
        yPos += 55;

        // Control Panel
        _controlPanel = new Guna2ShadowPanel
        {
            Location = new Point(10, yPos),
            Size = new Size(780, 250),
            FillColor = Color.FromArgb(31, 42, 51),
            ShadowColor = Color.Black,
            ShadowDepth = 50,
            ShadowShift = 3
        };
        _mainPanel.Controls.Add(_controlPanel);

        CreateControlPanelControls();

        yPos += 270;

        // Status
        _lblStatus = new Label
        {
            Text = "Status: Initializing...",
            Location = new Point(15, yPos),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(196, 175, 142)
        };
        _mainPanel.Controls.Add(_lblStatus);
        yPos += 35;

        // Transcript Panel
        _transcriptPanel = new Guna2ShadowPanel
        {
            Location = new Point(10, yPos),
            Size = new Size(780, 380),
            FillColor = Color.FromArgb(31, 42, 51),
            ShadowColor = Color.Black,
            ShadowDepth = 50,
            ShadowShift = 3
        };
        _mainPanel.Controls.Add(_transcriptPanel);

        CreateTranscriptPanelControls();
    }

    private void CreateControlPanelControls()
    {
        // Source label
        _lblSourceLabel = new Label
        {
            Text = "Audio Source",
            Location = new Point(25, 25),
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(196, 175, 142)
        };
        _controlPanel.Controls.Add(_lblSourceLabel);

        // Source dropdown
        _cmbAudioSources = new Guna2ComboBox
        {
            Location = new Point(25, 52),
            Size = new Size(340, 36),
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(244, 240, 234),
            FillColor = Color.FromArgb(58, 71, 80),
            BorderRadius = 8,
            BorderColor = Color.FromArgb(196, 175, 142)
        };
        _controlPanel.Controls.Add(_cmbAudioSources);

        // Enable Speaker Detection checkbox
        _chkEnableSpeakerDetection = new Guna2CheckBox
        {
            Location = new Point(25, 95),
            Size = new Size(200, 20),
            Text = "Enable Speaker Detection",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(196, 175, 142),
            Checked = true,
            CheckedState =
        {
            BorderColor = Color.FromArgb(196, 175, 142),
            FillColor = Color.FromArgb(196, 175, 142)
        }
        };
        _controlPanel.Controls.Add(_chkEnableSpeakerDetection);

        // Model label
        _lblModel = new Label
        {
            Text = "Model Quality",
            Location = new Point(390, 25),
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(196, 175, 142)
        };
        _controlPanel.Controls.Add(_lblModel);

        // Model dropdown
        _cmbModel = new Guna2ComboBox
        {
            Location = new Point(390, 52),
            Size = new Size(170, 36),
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(244, 240, 234),
            FillColor = Color.FromArgb(58, 71, 80),
            BorderRadius = 8,
            BorderColor = Color.FromArgb(196, 175, 142)
        };
        _cmbModel.Items.AddRange(new object[] { "Tiny (Fast)", "Base (Balanced)", "Small (Accurate)" });
        _cmbModel.SelectedIndex = 1;
        _cmbModel.SelectedIndexChanged += CmbModel_SelectedIndexChanged;
        _controlPanel.Controls.Add(_cmbModel);

        // Chunk size label
        _lblChunkSize = new Label
        {
            Text = "Chunk Size",
            Location = new Point(585, 25),
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(196, 175, 142)
        };
        _controlPanel.Controls.Add(_lblChunkSize);

        // Chunk size dropdown
        _cmbChunkSize = new Guna2ComboBox
        {
            Location = new Point(585, 52),
            Size = new Size(170, 36),
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(244, 240, 234),
            FillColor = Color.FromArgb(58, 71, 80),
            BorderRadius = 8,
            BorderColor = Color.FromArgb(196, 175, 142)
        };
        _cmbChunkSize.Items.AddRange(new object[] { "5 seconds", "10 seconds", "15 seconds", "20 seconds" });
        _cmbChunkSize.SelectedIndex = 0;
        _controlPanel.Controls.Add(_cmbChunkSize);

        // Buttons
        int btnY = 110;

        _btnStart = CreateGunaButton("▶  Start Recording", new Point(25, btnY), Color.FromArgb(196, 175, 142));
        _btnStart.Click += BtnStart_Click;
        _controlPanel.Controls.Add(_btnStart);

        _btnStop = CreateGunaButton("⏹  Stop Recording", new Point(230, btnY), Color.FromArgb(217, 201, 167));
        _btnStop.Click += BtnStop_Click;
        _btnStop.Enabled = false;
        _controlPanel.Controls.Add(_btnStop);

        _btnRefresh = CreateGunaButton("🔄  Refresh", new Point(435, btnY), Color.FromArgb(58, 71, 80));
        _btnRefresh.Click += BtnRefresh_Click;
        _controlPanel.Controls.Add(_btnRefresh);

        // Queue status
        _lblQueueStatus = new Label
        {
            Text = "Queue: 0 pending | 0 processed | 0 skipped",
            Location = new Point(25, 170),
            AutoSize = false,
            Size = new Size(730, 25),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(217, 201, 167),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _controlPanel.Controls.Add(_lblQueueStatus);

        // Performance info
        var lblPerfInfo = new Label
        {
            Text = "💡 Tiny = 4x faster, less accurate | Base = balanced | Small = slower, more accurate\n⚡ Larger chunks = less CPU usage | Queue skips chunks if falling behind",
            Location = new Point(25, 200),
            AutoSize = false,
            Size = new Size(730, 40),
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(217, 201, 167)
        };
        _controlPanel.Controls.Add(lblPerfInfo);
    }

    private void CreateTranscriptPanelControls()
    {
        _lblTranscriptTitle = new Label
        {
            Text = "Live Transcription",
            Location = new Point(25, 25),
            AutoSize = true,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(196, 175, 142)
        };
        _transcriptPanel.Controls.Add(_lblTranscriptTitle);

        _lblTranscriptTitle = new Label
        {
            Text = "Live Transcription",
            Location = new Point(25, 25),
            AutoSize = true,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(196, 175, 142)
        };
        _transcriptPanel.Controls.Add(_lblTranscriptTitle);

        // NEW: Edit Speakers Button
        _btnEditSpeakers = new Guna2Button
        {
            Location = new Point(600, 20),
            Size = new Size(155, 35),
            Text = "✏️ Edit Speakers",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 26, 26),
            FillColor = Color.FromArgb(196, 175, 142),
            BorderRadius = 6,
            Cursor = Cursors.Hand,
            Visible = false // Show only when speaker detection is on
        };
        _btnEditSpeakers.Click += BtnEditSpeakers_Click;
        _transcriptPanel.Controls.Add(_btnEditSpeakers);


        _txtTranscript = new Guna2TextBox
        {
            Location = new Point(25, 60),
            Size = new Size(730, 250),
            Multiline = true,
            ReadOnly = true,
            Font = new Font("Segoe UI", 10F),
            BorderRadius = 8,
            BorderColor = Color.FromArgb(196, 175, 142),
            FillColor = Color.FromArgb(58, 71, 80),
            ForeColor = Color.FromArgb(244, 240, 234),
            PlaceholderText = "Transcription will appear here as you record...",
            ScrollBars = ScrollBars.Vertical
        };
        _transcriptPanel.Controls.Add(_txtTranscript);

        _lblInfo = new Label
        {
            Text = "💡 Transcription appears in real-time based on chunk size\n⚠️ First run downloads selected Whisper model",
            Location = new Point(25, 320),
            AutoSize = false,
            Size = new Size(730, 45),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(217, 201, 167)
        };
        _transcriptPanel.Controls.Add(_lblInfo);
    }

    private Guna2Button CreateGunaButton(string text, Point location, Color color)
    {
        var btn = new Guna2Button
        {
            Location = location,
            Size = new Size(195, 45),
            Text = text,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 26, 26),
            FillColor = color,
            BorderRadius = 8,
            Cursor = Cursors.Hand
        };

        btn.HoverState.FillColor = Color.FromArgb(
            Math.Min(255, color.R + 15),
            Math.Min(255, color.G + 15),
            Math.Min(255, color.B + 15)
        );

        return btn;
    }

    private void LoadAudioSources()
    {
        _audioSources = _audioSourceProvider.GetAvailableAudioSources();
        _cmbAudioSources.Items.Clear();

        foreach (var source in _audioSources)
        {
            _cmbAudioSources.Items.Add(source.DisplayName);
        }

        if (_cmbAudioSources.Items.Count > 0)
        {
            _cmbAudioSources.SelectedIndex = 0;
        }
    }

    private void RestoreSettings()
    {
        // Restore model selection
        _cmbModel.SelectedIndex = _settings.SelectedModel switch
        {
            GgmlType.Tiny => 0,
            GgmlType.Small => 2,
            _ => 1
        };

        // Restore chunk size
        _cmbChunkSize.SelectedIndex = _settings.ChunkDurationSeconds switch
        {
            10 => 1,
            15 => 2,
            20 => 3,
            _ => 0
        };

        // Restore audio source if available
        var sourceIndex = _cmbAudioSources.Items.IndexOf(_settings.LastAudioSource);
        if (sourceIndex >= 0)
        {
            _cmbAudioSources.SelectedIndex = sourceIndex;
        }

        // Restore speaker detection setting
        _chkEnableSpeakerDetection.Checked = _settings.EnableSpeakerDetection;
    }

    private void SaveCurrentSettings()
    {
        _settings.SelectedModel = _cmbModel.SelectedIndex switch
        {
            0 => GgmlType.Tiny,
            2 => GgmlType.Small,
            _ => GgmlType.Base
        };

        _settings.ChunkDurationSeconds = _cmbChunkSize.SelectedIndex switch
        {
            1 => 10,
            2 => 15,
            3 => 20,
            _ => 5
        };

        if (_cmbAudioSources.SelectedItem != null)
        {
            _settings.LastAudioSource = _cmbAudioSources.SelectedItem.ToString()!;
        }

        _settings.EnableSpeakerDetection = _chkEnableSpeakerDetection.Checked; // NEW

        _settingsService.Save(_settings);
    }

    private async Task InitializeWhisperAsync()
    {
        UpdateStatus("Initializing Whisper model...");

        try
        {
            var model = _settings.SelectedModel;
            string modelSize = model switch
            {
                GgmlType.Tiny => "~75MB",
                GgmlType.Small => "~466MB",
                _ => "~140MB"
            };

            UpdateStatus($"Downloading Whisper {model} model ({modelSize})...");
            await _transcriptionService.InitializeAsync(model);

            UpdateStatus($"Ready ✓ (Using {model} model)");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error initializing Whisper: {ex.Message}");
        }
    }

    private void CmbModel_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var newModel = _cmbModel.SelectedIndex switch
        {
            0 => GgmlType.Tiny,
            2 => GgmlType.Small,
            _ => GgmlType.Base
        };

        if (newModel != _settings.SelectedModel)
        {
            _settings.SelectedModel = newModel;
            Task.Run(async () => await InitializeWhisperAsync());
        }
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        StartRecordingFromTray();
    }

    public void StartRecordingFromTray()
    {
        if (_cmbAudioSources.SelectedIndex < 0)
        {
            MessageBox.Show("Please select an audio source first.", "No Source Selected",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _fullTranscript.Clear();
        _transcriptWithSpeakers = new TranscriptWithSpeakers(); // NEW
        _speakerDetectionService.Reset(); // NEW
        _txtTranscript.Text = "";
        _btnEditSpeakers.Visible = false; // NEW

        // Restore saved speaker names
        foreach (var kvp in _settings.SpeakerNames)
        {
            _transcriptWithSpeakers.SpeakerNames[kvp.Key] = kvp.Value;
        }

        var settings = new TranscriptionSettings
        {
            Model = _settings.SelectedModel,
            ChunkDurationSeconds = _cmbChunkSize.SelectedIndex switch
            {
                1 => 10,
                2 => 15,
                3 => 20,
                _ => 5
            },
            AudioSourceName = _cmbAudioSources.Text
        };

        _audioCaptureService.StartRecording(settings);

        _btnStart.Enabled = false;
        _btnStop.Enabled = true;
        _cmbAudioSources.Enabled = false;
        _cmbChunkSize.Enabled = false;
        _cmbModel.Enabled = false;
        _btnRefresh.Enabled = false;
        _chkEnableSpeakerDetection.Enabled = false; // NEW

        _systemTrayService.SetRecordingState(true);

        var speakerStatus = _chkEnableSpeakerDetection.Checked ? " with speaker detection" : "";
        UpdateStatus($"🔴 Recording from '{_cmbAudioSources.Text}' ({settings.ChunkDurationSeconds}s chunks{speakerStatus})...");

        SaveCurrentSettings();
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        StopRecordingFromTray();
    }

    public void StopRecordingFromTray()
    {
        _audioCaptureService.StopRecording();
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        LoadAudioSources();
        UpdateStatus("Audio sources refreshed");
    }

    private async void OnChunkReady(object? sender, (string audioPath, TimeSpan timestamp) data)
    {
        try
        {
            var transcription = await _transcriptionService.TranscribeAsync(data.audioPath);

            if (!string.IsNullOrWhiteSpace(transcription))
            {
                if (_chkEnableSpeakerDetection.Checked)
                {
                    // Analyze speaker
                    var segment = _speakerDetectionService.AnalyzeAudioSegment(
                        data.audioPath,
                        transcription,
                        data.timestamp);

                    _transcriptWithSpeakers.Segments.Add(segment);

                    // Update speaker names from saved settings
                    foreach (var kvp in _settings.SpeakerNames)
                    {
                        _transcriptWithSpeakers.SpeakerNames[kvp.Key] = kvp.Value;
                    }

                    this.Invoke(new Action(() =>
                    {
                        _txtTranscript.Text = _transcriptWithSpeakers.GetFormattedTranscript();
                        _btnEditSpeakers.Visible = true;
                    }));
                }
                else
                {
                    // No speaker detection - simple append
                    _fullTranscript.Append(" ");
                    _fullTranscript.Append(transcription);

                    this.Invoke(new Action(() =>
                    {
                        _txtTranscript.Text = _fullTranscript.ToString().Trim();
                    }));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error transcribing chunk: {ex.Message}");
        }
    }

    private void BtnEditSpeakers_Click(object? sender, EventArgs e)
    {
        var speakerIds = _transcriptWithSpeakers.Segments
            .Select(s => s.SpeakerId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (speakerIds.Count == 0)
        {
            MessageBox.Show("No speakers detected yet.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new Form
        {
            Text = "Edit Speaker Names",
            Size = new Size(400, 300),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(244, 240, 234)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true
        };
        form.Controls.Add(panel);

        var textBoxes = new Dictionary<int, TextBox>();
        int yPos = 10;

        foreach (var speakerId in speakerIds)
        {
            var currentName = _transcriptWithSpeakers.SpeakerNames.ContainsKey(speakerId)
                ? _transcriptWithSpeakers.SpeakerNames[speakerId]
                : $"Speaker {speakerId}";

            var label = new Label
            {
                Text = $"Speaker {speakerId}:",
                Location = new Point(10, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 42, 51)
            };
            panel.Controls.Add(label);

            var textBox = new TextBox
            {
                Location = new Point(120, yPos - 3),
                Size = new Size(220, 25),
                Text = currentName,
                Font = new Font("Segoe UI", 10F)
            };
            textBoxes[speakerId] = textBox;
            panel.Controls.Add(textBox);

            yPos += 40;
        }

        var btnSave = new Button
        {
            Text = "Save",
            Location = new Point(150, yPos + 20),
            Size = new Size(100, 35),
            DialogResult = DialogResult.OK
        };
        panel.Controls.Add(btnSave);

        if (form.ShowDialog() == DialogResult.OK)
        {
            foreach (var kvp in textBoxes)
            {
                var newName = kvp.Value.Text.Trim();
                if (!string.IsNullOrEmpty(newName))
                {
                    _transcriptWithSpeakers.SpeakerNames[kvp.Key] = newName;
                    _settings.SpeakerNames[kvp.Key] = newName;
                }
            }

            _settingsService.Save(_settings);
            _txtTranscript.Text = _transcriptWithSpeakers.GetFormattedTranscript();
            UpdateStatus("Speaker names updated");
        }
    }

    private void OnQueueStatsUpdated(object? sender, QueueStatistics stats)
    {
        this.Invoke(new Action(() =>
        {
            _lblQueueStatus.Text = $"Queue: {stats.PendingChunks} pending | {stats.ProcessedChunks} processed | {stats.SkippedChunks} skipped";
        }));
    }

    private void OnRecordingStopped(object? sender, EventArgs e)
    {
        this.Invoke(new Action(() =>
        {
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            _cmbAudioSources.Enabled = true;
            _cmbChunkSize.Enabled = true;
            _cmbModel.Enabled = true;
            _btnRefresh.Enabled = true;
            _chkEnableSpeakerDetection.Enabled = true; // NEW

            _systemTrayService.SetRecordingState(false);
            UpdateStatus("⏳ Processing remaining audio...");
        }));

        Task.Run(async () =>
        {
            await Task.Delay(2000);
            PromptToSaveTranscript();
        });
    }

    private void PromptToSaveTranscript()
    {
        string transcription = _fullTranscript.ToString().Trim();

        if (string.IsNullOrWhiteSpace(transcription))
        {
            UpdateStatus("No speech detected");
            return;
        }

        UpdateStatus("Ready to save transcript");

        this.Invoke(new Action(() =>
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string defaultFileName = $"transcript_{timestamp}.md";

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                saveDialog.DefaultExt = "md";
                saveDialog.FileName = defaultFileName;
                saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                saveDialog.Title = "Save Transcript";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveTranscriptToFile(saveDialog.FileName, transcription);
                }
                else
                {
                    UpdateStatus("Save cancelled");
                }
            }
        }));
    }

    private void SaveTranscriptToFile(string filePath, string transcription)
    {
        try
        {
            var markdown = new System.Text.StringBuilder();
            markdown.AppendLine("# Audio Transcript");
            markdown.AppendLine();
            markdown.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            markdown.AppendLine();

            if (_chkEnableSpeakerDetection.Checked && _transcriptWithSpeakers.Segments.Count > 0)
            {
                markdown.AppendLine("## Speakers");
                foreach (var kvp in _transcriptWithSpeakers.SpeakerNames.OrderBy(k => k.Key))
                {
                    markdown.AppendLine($"- **Speaker {kvp.Key}:** {kvp.Value}");
                }
                markdown.AppendLine();
                markdown.AppendLine("## Transcription");
                markdown.AppendLine();
                markdown.AppendLine(_transcriptWithSpeakers.GetFormattedTranscript());
            }
            else
            {
                markdown.AppendLine("## Transcription");
                markdown.AppendLine();
                markdown.AppendLine(transcription);
            }

            File.WriteAllText(filePath, markdown.ToString());

            UpdateStatus($"✓ Saved to {Path.GetFileName(filePath)}");
            MessageBox.Show($"Transcript saved successfully!\n\nLocation: {filePath}",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving transcript: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateStatus(string status)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => UpdateStatus(status)));
            return;
        }
        _lblStatus.Text = $"Status: {status}";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // SystemTrayService handles minimize-to-tray
        // This only fires on actual exit
        if (e.CloseReason != CloseReason.UserClosing)
        {
            SaveCurrentSettings();
        }

        base.OnFormClosing(e);
    }
}