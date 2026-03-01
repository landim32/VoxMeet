using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using VoxMeet.Models;
using VoxMeet.Services;

namespace VoxMeet;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly AudioService _audioService;
    private readonly TranscriptionService _transcriptionService;
    private readonly ChatGPTService _chatGPTService;
    private readonly SimulationService _simulationService;
    private readonly StringBuilder _questionBuffer = new();
    private AppSettings _settings;
    private bool _isRunning;
    private bool _isSimulation;

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            OnPropertyChanged();
            BtnStartStop.Background = value
                ? new SolidColorBrush(Color.FromArgb(0xCC, 0xEF, 0x53, 0x50))
                : new SolidColorBrush(Color.FromArgb(0xCC, 0x4C, 0xAF, 0x50));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        TitleText.Text = $"VoxMeet v{version?.Major}.{version?.Minor}.{version?.Build}";

        _settings = AppSettings.Load();
        ApplyAppearance();

        _audioService = new AudioService();
        _transcriptionService = new TranscriptionService();
        _chatGPTService = new ChatGPTService();
        _simulationService = new SimulationService();

        _audioService.AudioChunkReady += OnAudioChunkReady;
        _transcriptionService.StatusChanged += OnStatusChanged;
        _transcriptionService.TranscriptionReceived += OnTranscriptionReceived;
        _transcriptionService.SilenceDetected += OnSilenceDetected;
        _transcriptionService.ErrorOccurred += OnTranscriptionError;

        _chatGPTService.AnswerReceived += OnAnswerReceived;
        _chatGPTService.ErrorOccurred += OnChatGPTError;

        Loaded += (_, _) =>
        {
            var screen = SystemParameters.WorkArea;
            Left = (screen.Width - Width) / 2;
            Top = screen.Height - Height - 20;
        };
    }

    private void ApplyAppearance()
    {
        try
        {
            var bgColor = (Color)ColorConverter.ConvertFromString(_settings.BackgroundColor);
            var alpha = (byte)(255 * _settings.BackgroundOpacity / 100);
            MainBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, bgColor.R, bgColor.G, bgColor.B));
        }
        catch
        {
            var alpha = (byte)(255 * _settings.BackgroundOpacity / 100);
            MainBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, 0x1E, 0x1E, 0x2E));
        }
    }

    private static SolidColorBrush ParseColor(string hex, Color fallback)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(fallback);
        }
    }

    private void AppendLog(string message)
    {
        if (!_settings.ShowLog) return;

        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var brush = ParseColor(_settings.LogFontColor, Color.FromRgb(0x6C, 0x70, 0x86));
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 2),
                FontSize = _settings.LogFontSize
            };
            paragraph.Inlines.Add(new Run($"[{timestamp}] {message}") { Foreground = brush });
            OutputDocument.Blocks.Add(paragraph);
            TrimOutput();
            OutputBox.ScrollToEnd();
        });
    }

    private void AppendTranscription(string text)
    {
        Dispatcher.Invoke(() =>
        {
            var brush = ParseColor(_settings.TranscriptionFontColor, Color.FromRgb(0xEE, 0xEE, 0xF0));
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 4, 0, 4),
                FontSize = _settings.TranscriptionFontSize,
                FontWeight = FontWeights.SemiBold
            };
            paragraph.Inlines.Add(new Run(text) { Foreground = brush });
            OutputDocument.Blocks.Add(paragraph);
            TrimOutput();
            OutputBox.ScrollToEnd();
        });
    }

    private void AppendAnswer(string text)
    {
        Dispatcher.Invoke(() =>
        {
            var brush = ParseColor(_settings.AnswerFontColor, Color.FromRgb(0x94, 0xE2, 0xD5));
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 6, 0, 6),
                FontSize = _settings.AnswerFontSize,
                FontWeight = FontWeights.Bold
            };
            paragraph.Inlines.Add(new Run($">> {text}") { Foreground = brush });
            OutputDocument.Blocks.Add(paragraph);
            TrimOutput();
            OutputBox.ScrollToEnd();
        });
    }

    private void TrimOutput()
    {
        while (OutputDocument.Blocks.Count > 300)
            OutputDocument.Blocks.Remove(OutputDocument.Blocks.FirstBlock);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void BtnStartStop_Click(object sender, RoutedEventArgs e)
    {
        if (IsRunning)
            StopTranscription();
        else
            StartTranscription();
    }

    private void StartTranscription()
    {
        _settings = AppSettings.Load();
        ApplyAppearance();

        if (_settings.SimulationMode)
        {
            _isSimulation = true;
            IsRunning = true;
            _simulationService.Start(AppendLog, AppendTranscription, AppendAnswer);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.OpenAiApiKey))
        {
            MessageBox.Show("Please configure your OpenAI API key in Settings.",
                "VoxMeet", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isSimulation = false;
        _transcriptionService.Configure(_settings.OpenAiApiKey, _settings.WhisperModel);
        _chatGPTService.Configure(_settings.OpenAiApiKey);
        _audioService.BufferSeconds = _settings.BufferSeconds;
        _questionBuffer.Clear();
        _audioService.Start(_settings.AudioMode, _settings.SelectedDeviceId);
        IsRunning = true;

        AppendLog("Recording started. Waiting for audio...");
    }

    private void StopTranscription()
    {
        if (_isSimulation)
            _simulationService.Stop();
        else
            _audioService.Stop();

        _isSimulation = false;
        IsRunning = false;
        _questionBuffer.Clear();
        AppendLog("Recording stopped.");
    }

    private void OnAudioChunkReady(byte[] wavData)
    {
        var sizeMb = wavData.Length / 1024.0 / 1024.0;
        AppendLog($"Audio chunk captured ({sizeMb:F2} MB). Sending to API...");

        _ = Task.Run(async () =>
        {
            await _transcriptionService.TranscribeChunkAsync(wavData);

            if (IsRunning)
                AppendLog("Waiting for audio...");
        });
    }

    private void OnStatusChanged(string status)
    {
        AppendLog(status);
    }

    private void OnTranscriptionReceived(string text)
    {
        AppendLog("Transcription received.");
        AppendTranscription(text);

        if (_questionBuffer.Length > 0)
            _questionBuffer.Append(' ');
        _questionBuffer.Append(text);

        if (ContainsQuestion(text))
        {
            var question = _questionBuffer.ToString().Trim();
            _questionBuffer.Clear();

            AppendLog($"Question detected: \"{question}\"");
            AppendLog("Sending question to ChatGPT...");

            _ = Task.Run(async () =>
            {
                await _chatGPTService.AskAsync(question);
            });
        }
    }

    private void OnSilenceDetected()
    {
        if (!_settings.SilenceDetection) return;
        if (_questionBuffer.Length == 0) return;

        var question = _questionBuffer.ToString().Trim();
        _questionBuffer.Clear();

        AppendLog($"Silence detected. Sending buffered text as question: \"{question}\"");
        AppendLog("Sending question to ChatGPT...");

        _ = Task.Run(async () =>
        {
            await _chatGPTService.AskAsync(question);
        });
    }

    private static bool ContainsQuestion(string text)
    {
        return text.Contains('?');
    }

    private void OnAnswerReceived(string answer)
    {
        AppendLog("ChatGPT answer received.");
        AppendAnswer(answer);
    }

    private void OnChatGPTError(Exception ex)
    {
        AppendLog($"ChatGPT Error: {ex.Message}");
    }

    private void OnTranscriptionError(Exception ex)
    {
        AppendLog($"Error: {ex.Message}");
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        bool wasRunning = IsRunning;
        if (wasRunning)
            StopTranscription();

        var settingsWindow = new SettingsWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        settingsWindow.ShowDialog();

        _settings = AppSettings.Load();
        ApplyAppearance();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        StopTranscription();
        _audioService.Dispose();
        _transcriptionService.Dispose();
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _audioService.Dispose();
        _transcriptionService.Dispose();
        base.OnClosed(e);
    }
}
