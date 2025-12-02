using System.Text;
using Chronicis.CaptureApp.Models;
using Chronicis.CaptureApp.Services;
using MaterialSkin;
using MaterialSkin.Controls;
using Whisper.net.Ggml;

namespace Chronicis.CaptureApp.UI;

public class MainForm : MaterialForm
{
    // Services (injected)
    private readonly IAudioSourceProvider _audioSourceProvider;
    private readonly IAudioCaptureService _audioCaptureService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ISettingsService _settingsService;
    private readonly ISystemTrayService _systemTrayService;
    private readonly ISpeakerDetectionService _speakerDetectionService;

    // UI Controls
    private readonly MaterialSkinManager _materialSkinManager;
    private ComboBox _cmbAudioSources = null!;
    private MaterialButton _btnStart = null!;
    private MaterialButton _btnStop = null!;
    private MaterialButton _btnRefresh = null!;
    private MaterialLabel _lblStatus = null!;
    private MaterialMultiLineTextBox _txtTranscript = null!;
    private MaterialLabel _lblTranscriptTitle = null!;
    private MaterialLabel _lblSourceLabel = null!;
    private ComboBox _cmbChunkSize = null!;
    private MaterialLabel _lblChunkSize = null!;
    private ComboBox _cmbModel = null!;
    private MaterialLabel _lblModel = null!;
    private MaterialLabel _lblQueueStatus = null!;
    private MaterialCheckbox _chkEnableSpeakerDetection = null!;
    private MaterialButton _btnEditSpeakers = null!;
    private Panel _mainPanel = null!;
    private MaterialCard _controlCard = null!;
    private MaterialCard _transcriptCard = null!;

    // State
    private AppSettings _settings = null!;
    private StringBuilder _fullTranscript = new();
    private List<AudioSource> _audioSources = new();
    private TranscriptWithSpeakers _transcriptWithSpeakers = new();
    private string? _sessionAudioPath;

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
        _materialSkinManager = MaterialSkinManager.Instance;

        InitializeComponent();
        InitializeServices();
    }


    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form settings
        this.ClientSize = new Size(900, 800);
        this.Text = "Chronicis Audio Capture";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = true;  // Changed to true
        this.MinimizeBox = true;  // NEW - explicitly enable
        this.FormBorderStyle = FormBorderStyle.Sizable;  // Changed from FixedSingle
        this.AutoScroll = true;
        this.BackColor = Color.FromArgb(45, 45, 48);  // Dark grey background

        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chronicis.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load icon: {ex.Message}");
        }

        CreateMaterialUI();

        this.ResumeLayout(false);
    }

    private void InitializeServices()
    {
        _settings = _settingsService.Load();
        _systemTrayService.Initialize(this);
        LoadAudioSources();
        RestoreSettings();

        _audioCaptureService.ChunkReady += OnChunkReady;
        _audioCaptureService.QueueStatsUpdated += OnQueueStatsUpdated;
        _audioCaptureService.RecordingStopped += OnRecordingStopped;
        _audioCaptureService.SessionAudioReady += OnSessionAudioReady;

        Task.Run(async () => await InitializeWhisperAsync());
    }

    private void CreateMaterialUI()
    {
        _mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20, 80, 20, 20)
        };
        this.Controls.Add(_mainPanel);

        int yPos = 10;

        // Control Card
        _controlCard = new MaterialCard
        {
            Location = new Point(20, yPos),
            Size = new Size(840, 320),
            BackColor = Color.FromArgb(40, 40, 40)
        };
        _mainPanel.Controls.Add(_controlCard);

        CreateControlCardControls();

        yPos += 340;

        // Status
        _lblStatus = new MaterialLabel
        {
            Text = "Status: Initializing...",
            Location = new Point(20, yPos),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Subtitle2,
            HighEmphasis = true
        };
        _mainPanel.Controls.Add(_lblStatus);
        yPos += 40;

        // Transcript Card
        _transcriptCard = new MaterialCard
        {
            Location = new Point(20, yPos),
            Size = new Size(840, 380),
            BackColor = Color.FromArgb(40, 40, 40)
        };
        _mainPanel.Controls.Add(_transcriptCard);

        CreateTranscriptCardControls();
    }

    private void CreateControlCardControls()
    {
        int yPos = 20;

        // Source label
        _lblSourceLabel = new MaterialLabel
        {
            Text = "Audio Source",
            Location = new Point(20, yPos),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.H6,
            HighEmphasis = true
        };
        _controlCard.Controls.Add(_lblSourceLabel);
        yPos += 35;

        // Source dropdown
        _cmbAudioSources = new ComboBox
        {
            Location = new Point(20, yPos),
            Size = new Size(400, 30),
            Font = new Font("Segoe UI", 10F),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(55, 55, 55),
            ForeColor = Color.White
        };
        _controlCard.Controls.Add(_cmbAudioSources);

        // Model label
        _lblModel = new MaterialLabel
        {
            Text = "Model Quality",
            Location = new Point(440, 20),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.H6,
            HighEmphasis = true
        };
        _controlCard.Controls.Add(_lblModel);

        // Model dropdown
        _cmbModel = new ComboBox
        {
            Location = new Point(440, 55),
            Size = new Size(180, 30),
            Font = new Font("Segoe UI", 10F),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(55, 55, 55),
            ForeColor = Color.White
        };
        _cmbModel.Items.AddRange(new object[] { "Tiny (Fast)", "Base (Balanced)", "Small (Accurate)" });
        _cmbModel.SelectedIndex = 1;
        _cmbModel.SelectedIndexChanged += CmbModel_SelectedIndexChanged;
        _controlCard.Controls.Add(_cmbModel);

        // Chunk size label
        _lblChunkSize = new MaterialLabel
        {
            Text = "Chunk Size",
            Location = new Point(640, 20),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.H6,
            HighEmphasis = true
        };
        _controlCard.Controls.Add(_lblChunkSize);

        // Chunk size dropdown
        _cmbChunkSize = new ComboBox
        {
            Location = new Point(640, 55),
            Size = new Size(180, 30),
            Font = new Font("Segoe UI", 10F),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(55, 55, 55),
            ForeColor = Color.White
        };
        _cmbChunkSize.Items.AddRange(new object[] { "5 seconds", "10 seconds", "15 seconds", "20 seconds" });
        _cmbChunkSize.SelectedIndex = 0;
        _controlCard.Controls.Add(_cmbChunkSize);

        yPos = 100;

        // Speaker detection checkbox
        _chkEnableSpeakerDetection = new MaterialCheckbox
        {
            Text = "Enable Speaker Detection (Free, Local AI)",
            Location = new Point(20, yPos),
            AutoSize = true,
            Checked = true
        };
        _controlCard.Controls.Add(_chkEnableSpeakerDetection);

        yPos = 140;

        // Buttons
        _btnStart = new MaterialButton
        {
            Location = new Point(20, yPos),
            Size = new Size(200, 45),
            Text = "▶  START RECORDING",
            Type = MaterialButton.MaterialButtonType.Contained,
            UseAccentColor = true
        };
        _btnStart.Click += BtnStart_Click;
        _controlCard.Controls.Add(_btnStart);

        _btnStop = new MaterialButton
        {
            Location = new Point(230, yPos),
            Size = new Size(200, 45),
            Text = "⏹  STOP RECORDING",
            Type = MaterialButton.MaterialButtonType.Contained,
            Enabled = false
        };
        _btnStop.Click += BtnStop_Click;
        _controlCard.Controls.Add(_btnStop);

        _btnRefresh = new MaterialButton
        {
            Location = new Point(440, yPos),
            Size = new Size(150, 45),
            Text = "🔄 REFRESH",
            Type = MaterialButton.MaterialButtonType.Outlined
        };
        _btnRefresh.Click += BtnRefresh_Click;
        _controlCard.Controls.Add(_btnRefresh);

        yPos = 200;

        // Queue status
        _lblQueueStatus = new MaterialLabel
        {
            Text = "Queue: 0 pending | 0 processed | 0 skipped",
            Location = new Point(20, yPos),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Body2
        };
        _controlCard.Controls.Add(_lblQueueStatus);

        yPos += 30;

        // Info labels
        var lblInfo1 = new MaterialLabel
        {
            Text = "💡 Tiny = 4x faster, less accurate | Base = balanced | Small = slower, more accurate",
            Location = new Point(20, yPos),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Caption
        };
        _controlCard.Controls.Add(lblInfo1);

        yPos += 25;

        var lblInfo2 = new MaterialLabel
        {
            Text = "⚡ Larger chunks = less CPU usage | Queue skips chunks if falling behind",
            Location = new Point(20, yPos),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Caption
        };
        _controlCard.Controls.Add(lblInfo2);
    }

    private void CreateTranscriptCardControls()
    {
        // Title
        _lblTranscriptTitle = new MaterialLabel
        {
            Text = "Live Transcription",
            Location = new Point(20, 20),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.H5,
            HighEmphasis = true
        };
        _transcriptCard.Controls.Add(_lblTranscriptTitle);

        // Edit speakers button
        _btnEditSpeakers = new MaterialButton
        {
            Location = new Point(660, 15),
            Size = new Size(160, 40),
            Text = "✏️ EDIT SPEAKERS",
            Type = MaterialButton.MaterialButtonType.Outlined,
            Visible = false
        };
        _btnEditSpeakers.Click += BtnEditSpeakers_Click;
        _transcriptCard.Controls.Add(_btnEditSpeakers);

        // Transcript text box
        _txtTranscript = new MaterialMultiLineTextBox
        {
            Location = new Point(20, 70),
            Size = new Size(800, 250),
            ReadOnly = true,
            BackColor = Color.FromArgb(50, 50, 50),
            Font = new Font("Segoe UI", 10F),
            Hint = "Transcription will appear here as you record..."
        };
        _transcriptCard.Controls.Add(_txtTranscript);

        // Info label
        var lblInfo = new MaterialLabel
        {
            Text = "💡 Transcription appears in real-time based on chunk size\n⚠️ First run downloads selected Whisper model",
            Location = new Point(20, 330),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Caption
        };
        _transcriptCard.Controls.Add(lblInfo);
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
        _cmbModel.SelectedIndex = _settings.SelectedModel switch
        {
            GgmlType.Tiny => 0,
            GgmlType.Small => 2,
            _ => 1
        };

        _cmbChunkSize.SelectedIndex = _settings.ChunkDurationSeconds switch
        {
            10 => 1,
            15 => 2,
            20 => 3,
            _ => 0
        };

        var sourceIndex = _cmbAudioSources.Items.IndexOf(_settings.LastAudioSource);
        if (sourceIndex >= 0)
        {
            _cmbAudioSources.SelectedIndex = sourceIndex;
        }

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

        _settings.EnableSpeakerDetection = _chkEnableSpeakerDetection.Checked;

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
        _transcriptWithSpeakers = new TranscriptWithSpeakers();
        _speakerDetectionService.Reset();
        _txtTranscript.Text = "";
        _btnEditSpeakers.Visible = false;

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
        _chkEnableSpeakerDetection.Enabled = false;

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
                    var segment = _speakerDetectionService.AnalyzeAudioSegment(
                        data.audioPath,
                        transcription,
                        data.timestamp);

                    _transcriptWithSpeakers.Segments.Add(segment);

                    foreach (var kvp in _settings.SpeakerNames)
                    {
                        _transcriptWithSpeakers.SpeakerNames[kvp.Key] = kvp.Value;
                    }

                    this.Invoke(new Action(() =>
                    {
                        _txtTranscript.Text = _transcriptWithSpeakers.GetFormattedTranscript();
                        _txtTranscript.SelectionStart = _txtTranscript.Text.Length;  // NEW
                        _txtTranscript.ScrollToCaret();  // NEW
                        _btnEditSpeakers.Visible = true;
                    }));
                }
                else
                {
                    _fullTranscript.Append(" ");
                    _fullTranscript.Append(transcription);

                    this.Invoke(new Action(() =>
                    {
                        _txtTranscript.Text = _fullTranscript.ToString().Trim();
                        _txtTranscript.SelectionStart = _txtTranscript.Text.Length;  // NEW
                        _txtTranscript.ScrollToCaret();  // NEW
                    }));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error transcribing chunk: {ex.Message}");
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
            _chkEnableSpeakerDetection.Enabled = true;

            _systemTrayService.SetRecordingState(false);
            UpdateStatus("⏳ Processing remaining audio...");
        }));

        Task.Run(async () =>
        {
            await Task.Delay(2000);
            PromptToSaveTranscript();
        });
    }

    private void OnSessionAudioReady(object? sender, string audioPath)
    {
        _sessionAudioPath = audioPath;

        this.Invoke(new Action(() =>
        {
            UpdateStatus($"✓ Audio file ready: {Path.GetFileName(audioPath)}");
        }));
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
            Size = new Size(450, 350),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(50, 50, 50)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true,
            BackColor = Color.FromArgb(50, 50, 50)
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
                Location = new Point(10, yPos + 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White
            };
            panel.Controls.Add(label);

            var textBox = new TextBox
            {
                Location = new Point(140, yPos),
                Size = new Size(270, 30),
                Text = currentName,
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            textBoxes[speakerId] = textBox;
            panel.Controls.Add(textBox);

            yPos += 45;
        }

        var btnSave = new Button
        {
            Text = "Save Changes",
            Location = new Point(160, yPos + 20),
            Size = new Size(130, 40),
            DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(255, 193, 7),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        btnSave.FlatAppearance.BorderSize = 0;
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

    private void PromptToSaveTranscript()
    {
        string transcription = _chkEnableSpeakerDetection.Checked && _transcriptWithSpeakers.Segments.Count > 0
            ? _transcriptWithSpeakers.GetFormattedTranscript()
            : _fullTranscript.ToString().Trim();

        if (string.IsNullOrWhiteSpace(transcription))
        {
            UpdateStatus("No speech detected");
            return;
        }

        UpdateStatus("Ready to save transcript and audio");

        this.Invoke(new Action(() =>
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string defaultFileName = $"session_{timestamp}";

            // NEW: Use FolderBrowserDialog to let user choose save location
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select folder to save transcript and audio";
                folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string folder = folderDialog.SelectedPath;

                    // Save transcript
                    string transcriptPath = Path.Combine(folder, $"{defaultFileName}.md");
                    SaveTranscriptToFile(transcriptPath, transcription);

                    // Save audio
                    if (!string.IsNullOrEmpty(_sessionAudioPath) && File.Exists(_sessionAudioPath))
                    {
                        string audioExtension = Path.GetExtension(_sessionAudioPath);
                        string audioDestination = Path.Combine(folder, $"{defaultFileName}{audioExtension}");

                        try
                        {
                            File.Move(_sessionAudioPath, audioDestination);
                            UpdateStatus($"✓ Saved transcript and audio to {folder}");
                            MessageBox.Show(
                                $"Session saved successfully!\n\nTranscript: {Path.GetFileName(transcriptPath)}\nAudio: {Path.GetFileName(audioDestination)}\n\nLocation: {folder}",
                                "Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error saving audio: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
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
            var markdown = new StringBuilder();
            markdown.AppendLine("# D&D Session Transcript");
            markdown.AppendLine();
            markdown.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // NEW: Include audio file reference
            if (!string.IsNullOrEmpty(_sessionAudioPath))
            {
                string audioFileName = Path.GetFileName(_sessionAudioPath)
                    .Replace(Path.GetExtension(_sessionAudioPath), Path.GetExtension(_sessionAudioPath));
                markdown.AppendLine($"**Audio:** {audioFileName}");
            }

            markdown.AppendLine();

            if (_chkEnableSpeakerDetection.Checked && _transcriptWithSpeakers.Segments.Count > 0)
            {
                markdown.AppendLine("## Speakers");
                foreach (var kvp in _transcriptWithSpeakers.SpeakerNames.OrderBy(k => k.Key))
                {
                    markdown.AppendLine($"- **Speaker {kvp.Key}:** {kvp.Value}");
                }
                markdown.AppendLine();
            }

            markdown.AppendLine("## Transcription");
            markdown.AppendLine();
            markdown.AppendLine(transcription);

            File.WriteAllText(filePath, markdown.ToString());
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
        if (e.CloseReason != CloseReason.UserClosing)
        {
            SaveCurrentSettings();
        }

        base.OnFormClosing(e);
    }
}