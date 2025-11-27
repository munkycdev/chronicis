using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.MediaFoundation;
using Whisper.net;
using Whisper.net.Ggml;
using Guna.UI2.WinForms;

namespace Chronicis.CaptureApp
{
    public class MainForm : Form
    {
        private Guna2ComboBox _cmbAudioSources;
        private Guna2Button _btnStart;
        private Guna2Button _btnStop;
        private Guna2Button _btnRefresh;
        private Label _lblStatus;
        private Guna2TextBox _txtTranscript;
        private Label _lblTranscriptTitle;
        private Label _lblSourceLabel;
        private Label _lblInfo;
        private Guna2Panel _mainPanel;
        private Guna2ShadowPanel _controlPanel;
        private Guna2ShadowPanel _transcriptPanel;
        private Label _lblHeader;
        private Guna2ComboBox _cmbChunkSize;
        private Label _lblChunkSize;
        private Guna2ComboBox _cmbModel;
        private Label _lblModel;
        private Label _lblQueueStatus;

        private WasapiLoopbackCapture _captureDevice;
        private MemoryStream _currentChunkStream;
        private WaveFileWriter _currentChunkWriter;
        private int _chunkDurationSeconds = 5;
        private int _bytesPerChunk;
        private int _currentChunkBytes;
        private WaveFormat _captureFormat;

        private WhisperProcessor _whisperProcessor;
        private string _modelPath;
        private bool _isInitialized;
        private bool _isProcessing;
        private StringBuilder _fullTranscript;
        private List<string> _tempFiles;
        private bool _isCapturing;
        private GgmlType _selectedModel = GgmlType.Base;

        // Queue management
        private Queue<string> _pendingChunks;
        private int _processedChunks;
        private int _skippedChunks;

        public MainForm()
        {
            _fullTranscript = new StringBuilder();
            _tempFiles = new List<string>();
            _pendingChunks = new Queue<string>();
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "ggml-base.bin");

            InitializeComponent();
            LoadAudioSources();
            Task.Run(async () => await InitializeWhisperAsync());
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

        private void CreateModernUI()
        {
            // Main container panel
            _mainPanel = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.FromArgb(244, 240, 234), // Soft Off-White from Chronicis
                Padding = new Padding(20)
            };
            this.Controls.Add(_mainPanel);

            int yPos = 10;

            // Header with dragon icon
            _lblHeader = new Label
            {
                Text = "🐉 Chronicis Audio Capture",
                Location = new Point(10, yPos),
                AutoSize = true,
                Font = new Font("Spellweaver Display", 20F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(196, 175, 142) // Beige-Gold from Chronicis
            };
            // Fallback to Segoe UI if Spellweaver not available
            if (_lblHeader.Font.Name != "Spellweaver Display")
            {
                _lblHeader.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            }
            _mainPanel.Controls.Add(_lblHeader);
            yPos += 55;

            // Control Panel Card
            _controlPanel = new Guna2ShadowPanel
            {
                Location = new Point(10, yPos),
                Size = new Size(780, 250),
                FillColor = Color.FromArgb(31, 42, 51), // Deep Blue-Grey from Chronicis
                ShadowColor = Color.Black,
                ShadowDepth = 50,
                ShadowShift = 3
            };
            _mainPanel.Controls.Add(_controlPanel);

            // Source label
            _lblSourceLabel = new Label
            {
                Text = "Audio Source",
                Location = new Point(25, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(196, 175, 142) // Beige-Gold
            };
            _controlPanel.Controls.Add(_lblSourceLabel);

            // Source dropdown
            _cmbAudioSources = new Guna2ComboBox
            {
                Location = new Point(25, 52),
                Size = new Size(340, 36),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(244, 240, 234),
                FillColor = Color.FromArgb(58, 71, 80), // Slate Grey
                BorderRadius = 8,
                BorderColor = Color.FromArgb(196, 175, 142)
            };
            _controlPanel.Controls.Add(_cmbAudioSources);

            // Model selection label
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

            _btnStart = CreateGunaButton("▶  Start Recording", new Point(25, btnY), Color.FromArgb(94, 148, 255));
            _btnStart.Click += BtnStart_Click;
            _controlPanel.Controls.Add(_btnStart);

            _btnStop = CreateGunaButton("⏹  Stop Recording", new Point(230, btnY), Color.FromArgb(242, 78, 30));
            _btnStop.Click += BtnStop_Click;
            _btnStop.Enabled = false;
            _controlPanel.Controls.Add(_btnStop);

            _btnRefresh = CreateGunaButton("🔄  Refresh", new Point(435, btnY), Color.FromArgb(125, 137, 149));
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
                ForeColor = Color.FromArgb(125, 137, 149),
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
                ForeColor = Color.FromArgb(125, 137, 149)
            };
            _controlPanel.Controls.Add(lblPerfInfo);

            yPos += 270;

            // Status
            _lblStatus = new Label
            {
                Text = "Status: Initializing...",
                Location = new Point(15, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(125, 137, 149)
            };
            _mainPanel.Controls.Add(_lblStatus);
            yPos += 35;

            // Transcript Panel Card
            _transcriptPanel = new Guna2ShadowPanel
            {
                Location = new Point(10, yPos),
                Size = new Size(780, 380),
                FillColor = Color.White,
                ShadowColor = Color.Black,
                ShadowDepth = 50,
                ShadowShift = 3
            };
            _mainPanel.Controls.Add(_transcriptPanel);

            _lblTranscriptTitle = new Label
            {
                Text = "Live Transcription",
                Location = new Point(25, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            _transcriptPanel.Controls.Add(_lblTranscriptTitle);

            _txtTranscript = new Guna2TextBox
            {
                Location = new Point(25, 60),
                Size = new Size(730, 250),
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10F),
                BorderRadius = 8,
                BorderColor = Color.FromArgb(213, 218, 223),
                FillColor = Color.FromArgb(247, 249, 252),
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
                ForeColor = Color.FromArgb(125, 137, 149)
            };
            _transcriptPanel.Controls.Add(_lblInfo);
        }

        private void CmbModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedModel = _cmbModel.SelectedIndex switch
            {
                0 => GgmlType.Tiny,
                1 => GgmlType.Base,
                2 => GgmlType.Small,
                _ => GgmlType.Base
            };

            // Update model path
            string modelFileName = _selectedModel switch
            {
                GgmlType.Tiny => "ggml-tiny.bin",
                GgmlType.Small => "ggml-small.bin",
                _ => "ggml-base.bin"
            };
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", modelFileName);

            // Reset initialization flag to reload model
            _isInitialized = false;
            _whisperProcessor?.Dispose();
            _whisperProcessor = null;
        }

        private Guna2Button CreateGunaButton(string text, Point location, Color color)
        {
            var btn = new Guna2Button
            {
                Location = location,
                Size = new Size(195, 45),
                Text = text,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                FillColor = color,
                BorderRadius = 8,
                Cursor = Cursors.Hand
            };

            btn.HoverState.FillColor = Color.FromArgb(
                Math.Max(0, color.R - 20),
                Math.Max(0, color.G - 20),
                Math.Max(0, color.B - 20)
            );

            return btn;
        }

        private void LoadAudioSources()
        {
            _cmbAudioSources.Items.Clear();
            _cmbAudioSources.Items.Add("System Audio (All Sounds)");

            try
            {
                var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                foreach (var device in devices)
                {
                    var sessionManager = device.AudioSessionManager;
                    var sessions = sessionManager.Sessions;

                    for (int i = 0; i < sessions.Count; i++)
                    {
                        var session = sessions[i];
                        var processId = session.GetProcessID;

                        if (processId != 0)
                        {
                            try
                            {
                                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                                string displayName = $"{process.ProcessName} (PID: {processId})";
                                if (!_cmbAudioSources.Items.Contains(displayName))
                                {
                                    _cmbAudioSources.Items.Add(displayName);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading sources: {ex.Message}");
            }

            if (_cmbAudioSources.Items.Count > 0)
            {
                _cmbAudioSources.SelectedIndex = 0;
            }
        }

        private async Task InitializeWhisperAsync()
        {
            UpdateStatus("Initializing Whisper model...");

            try
            {
                if (!File.Exists(_modelPath))
                {
                    var modelDir = Path.GetDirectoryName(_modelPath);
                    if (!Directory.Exists(modelDir))
                    {
                        Directory.CreateDirectory(modelDir);
                    }

                    string modelSize = _selectedModel switch
                    {
                        GgmlType.Tiny => "~75MB",
                        GgmlType.Small => "~466MB",
                        _ => "~140MB"
                    };

                    UpdateStatus($"Downloading Whisper {_selectedModel} model ({modelSize})...");
                    using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(_selectedModel);
                    using var fileWriter = File.OpenWrite(_modelPath);
                    await modelStream.CopyToAsync(fileWriter);
                }

                await Task.Run(() =>
                {
                    var factory = WhisperFactory.FromPath(_modelPath);
                    _whisperProcessor = factory.CreateBuilder()
                        .WithLanguage("auto")
                        .Build();
                });

                _isInitialized = true;
                UpdateStatus($"Ready ✓ (Using {_selectedModel} model)");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error initializing Whisper: {ex.Message}");
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (_cmbAudioSources.SelectedIndex < 0)
            {
                MessageBox.Show("Please select an audio source first.", "No Source Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            StartRecording();
        }

        private void StartRecording()
        {
            try
            {
                _fullTranscript.Clear();
                _tempFiles.Clear();
                _pendingChunks.Clear();
                _processedChunks = 0;
                _skippedChunks = 0;

                // Get chunk duration from dropdown
                _chunkDurationSeconds = _cmbChunkSize.SelectedIndex switch
                {
                    0 => 5,
                    1 => 10,
                    2 => 15,
                    3 => 20,
                    _ => 5
                };

                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                _captureDevice = new WasapiLoopbackCapture(device);
                _captureFormat = _captureDevice.WaveFormat;
                _bytesPerChunk = _captureFormat.AverageBytesPerSecond * _chunkDurationSeconds;
                _currentChunkBytes = 0;

                CreateNewChunk();

                _captureDevice.DataAvailable += OnDataAvailable;
                _captureDevice.RecordingStopped += OnRecordingStopped;

                _captureDevice.StartRecording();
                _isCapturing = true;

                _btnStart.Enabled = false;
                _btnStop.Enabled = true;
                _cmbAudioSources.Enabled = false;
                _cmbChunkSize.Enabled = false;
                _cmbModel.Enabled = false;
                _btnRefresh.Enabled = false;

                _txtTranscript.Text = "";
                UpdateStatus($"🔴 Recording from '{_cmbAudioSources.Text}' ({_chunkDurationSeconds}s chunks, {_selectedModel} model)...");
                UpdateQueueStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting recording: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopRecording();
        }

        private void StopRecording()
        {
            if (_captureDevice != null)
            {
                _captureDevice.StopRecording();
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_currentChunkWriter == null)
                return;

            _currentChunkWriter.Write(e.Buffer, 0, e.BytesRecorded);
            _currentChunkBytes += e.BytesRecorded;

            if (_currentChunkBytes >= _bytesPerChunk)
            {
                ProcessCurrentChunk();
                CreateNewChunk();
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            _isCapturing = false;

            this.Invoke(new Action(() =>
            {
                _btnStart.Enabled = true;
                _btnStop.Enabled = false;
                _cmbAudioSources.Enabled = true;
                _cmbChunkSize.Enabled = true;
                _cmbModel.Enabled = true;
                _btnRefresh.Enabled = true;
            }));

            UpdateStatus("⏳ Processing remaining audio...");

            if (_currentChunkBytes > 0)
            {
                ProcessCurrentChunk();
            }

            Task.Run(async () =>
            {
                // Wait for queue to empty
                int maxWaitSeconds = 60;
                int waitedSeconds = 0;
                while ((_isProcessing || _pendingChunks.Count > 0) && waitedSeconds < maxWaitSeconds)
                {
                    await Task.Delay(500);
                    waitedSeconds++;
                }

                await Task.Delay(2000);
                PromptToSaveTranscript();
            });
        }

        private void CreateNewChunk()
        {
            _currentChunkStream = new MemoryStream();
            _currentChunkWriter = new WaveFileWriter(_currentChunkStream, _captureFormat);
            _currentChunkBytes = 0;
        }

        private void ProcessCurrentChunk()
        {
            _currentChunkWriter?.Flush();

            // Write chunk directly to temp file (don't store in memory)
            string chunkPath = Path.Combine(Path.GetTempPath(), $"chunk_{Guid.NewGuid()}.wav");
            File.WriteAllBytes(chunkPath, _currentChunkStream.ToArray());

            _currentChunkWriter?.Dispose();
            _currentChunkStream?.Dispose();

            // Add to queue
            _pendingChunks.Enqueue(chunkPath);
            UpdateQueueStatus();

            // Start processing if not already processing
            if (!_isProcessing)
            {
                Task.Run(async () => await ProcessQueueAsync());
            }
        }

        private async Task ProcessQueueAsync()
        {
            while (_pendingChunks.Count > 0)
            {
                // If queue is building up (more than 3 chunks behind), skip some chunks
                if (_pendingChunks.Count > 3)
                {
                    var skipPath = _pendingChunks.Dequeue();
                    CleanupTempFile(skipPath);
                    _skippedChunks++;
                    UpdateQueueStatus();
                    UpdateStatus($"⚠️ Queue backup - skipped chunk {_skippedChunks} to maintain real-time");
                    continue;
                }

                var chunkPath = _pendingChunks.Dequeue();
                UpdateQueueStatus();
                await ProcessAudioChunkAsync(chunkPath);
            }
        }

        private async Task ProcessAudioChunkAsync(string chunkPath)
        {
            _isProcessing = true;
            string whisperInputFile = null;

            try
            {
                whisperInputFile = ConvertToWhisperFormat(chunkPath);
                _tempFiles.Add(whisperInputFile);

                string transcription = await TranscribeAsync(whisperInputFile);

                if (!string.IsNullOrWhiteSpace(transcription))
                {
                    _fullTranscript.Append(" ");
                    _fullTranscript.Append(transcription);

                    this.Invoke(new Action(() =>
                    {
                        _txtTranscript.Text = _fullTranscript.ToString().Trim();
                    }));
                }

                _processedChunks++;
                UpdateQueueStatus();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing chunk: {ex.Message}");
            }
            finally
            {
                CleanupTempFile(chunkPath);
                _isProcessing = false;
            }
        }

        private string ConvertToWhisperFormat(string inputWavPath)
        {
            string outputPath = Path.Combine(Path.GetTempPath(),
                $"whisper_{Path.GetFileNameWithoutExtension(inputWavPath)}.wav");

            using (var reader = new WaveFileReader(inputWavPath))
            {
                var outFormat = new WaveFormat(16000, 1);
                using (var resampler = new MediaFoundationResampler(reader, outFormat))
                {
                    WaveFileWriter.CreateWaveFile(outputPath, resampler);
                }
            }
            return outputPath;
        }

        private async Task<string> TranscribeAsync(string audioFilePath)
        {
            if (!_isInitialized)
                await InitializeWhisperAsync();

            using var fileStream = File.OpenRead(audioFilePath);
            var segments = _whisperProcessor.ProcessAsync(fileStream).ConfigureAwait(false);
            var transcription = string.Empty;

            await foreach (var segment in segments)
            {
                transcription += segment.Text;
            }

            return transcription.Trim();
        }

        private void PromptToSaveTranscript()
        {
            string transcription = _fullTranscript.ToString().Trim();

            if (string.IsNullOrWhiteSpace(transcription))
            {
                UpdateStatus("No speech detected");
                CleanupAllTempFiles();
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
                        CleanupAllTempFiles();
                    }
                }
            }));
        }

        private void SaveTranscriptToFile(string filePath, string transcription)
        {
            try
            {
                var markdown = new StringBuilder();
                markdown.AppendLine("# Audio Transcript");
                markdown.AppendLine();
                markdown.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                markdown.AppendLine();
                markdown.AppendLine("## Transcription");
                markdown.AppendLine();
                markdown.AppendLine(transcription);

                File.WriteAllText(filePath, markdown.ToString());
                CleanupAllTempFiles();

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

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadAudioSources();
            UpdateStatus("Audio sources refreshed");
        }

        private void UpdateQueueStatus()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateQueueStatus()));
                return;
            }
            _lblQueueStatus.Text = $"Queue: {_pendingChunks.Count} pending | {_processedChunks} processed | {_skippedChunks} skipped";
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

        private void CleanupTempFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { }
            }
        }

        private void CleanupAllTempFiles()
        {
            foreach (var file in _tempFiles)
            {
                CleanupTempFile(file);
            }
            _tempFiles.Clear();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isCapturing)
            {
                var result = MessageBox.Show(
                    "Recording is still in progress. Stop and save before closing?",
                    "Confirm Close",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    StopRecording();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _captureDevice?.Dispose();
            _currentChunkWriter?.Dispose();
            _currentChunkStream?.Dispose();
            _whisperProcessor?.Dispose();
            CleanupAllTempFiles();

            base.OnFormClosing(e);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}