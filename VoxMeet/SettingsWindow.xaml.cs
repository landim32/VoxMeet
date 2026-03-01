using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VoxMeet.Models;
using VoxMeet.Services;

namespace VoxMeet;

public class DeviceItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        LoadSettingsToUi();
    }

    private void LoadSettingsToUi()
    {
        // General
        TxtApiKey.Text = _settings.OpenAiApiKey ?? "";

        foreach (ComboBoxItem item in CmbAudioMode.Items)
        {
            if ((string)item.Tag == _settings.AudioMode)
            {
                CmbAudioMode.SelectedItem = item;
                break;
            }
        }

        foreach (ComboBoxItem item in CmbBufferSeconds.Items)
        {
            if ((string)item.Tag == _settings.BufferSeconds.ToString())
            {
                CmbBufferSeconds.SelectedItem = item;
                break;
            }
        }

        foreach (ComboBoxItem item in CmbWhisperModel.Items)
        {
            if ((string)item.Tag == _settings.WhisperModel)
            {
                CmbWhisperModel.SelectedItem = item;
                break;
            }
        }

        PopulateDevices();

        // Appearance
        TxtBgColor.Text = _settings.BackgroundColor;
        SliderOpacity.Value = _settings.BackgroundOpacity;
        TxtOpacityValue.Text = $"{_settings.BackgroundOpacity}%";

        SliderTranscriptionFont.Value = _settings.TranscriptionFontSize;
        TxtTranscriptionFontValue.Text = _settings.TranscriptionFontSize.ToString();
        TxtTranscriptionColor.Text = _settings.TranscriptionFontColor;

        SliderAnswerFont.Value = _settings.AnswerFontSize;
        TxtAnswerFontValue.Text = _settings.AnswerFontSize.ToString();
        TxtAnswerColor.Text = _settings.AnswerFontColor;

        ChkShowLog.IsChecked = _settings.ShowLog;
        SliderLogFont.Value = _settings.LogFontSize;
        TxtLogFontValue.Text = _settings.LogFontSize.ToString();
        TxtLogColor.Text = _settings.LogFontColor;

        // Behavior
        ChkSilenceDetection.IsChecked = _settings.SilenceDetection;
        ChkSimulationMode.IsChecked = _settings.SimulationMode;

        // Prompt
        TxtSystemPrompt.Text = _settings.SystemPrompt;
    }

    // --- General tab ---

    private void CmbAudioMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PopulateDevices();
    }

    private void PopulateDevices()
    {
        if (CmbDevice == null) return;

        var selectedMode = (CmbAudioMode.SelectedItem as ComboBoxItem)?.Tag as string ?? "loopback";

        var devices = selectedMode == "loopback"
            ? AudioService.GetOutputDevices()
            : AudioService.GetInputDevices();

        var items = new List<DeviceItem>
        {
            new() { Id = "", Name = "(System Default)" }
        };
        items.AddRange(devices.Select(d => new DeviceItem { Id = d.Id, Name = d.Name }));

        CmbDevice.ItemsSource = items;

        var match = items.FirstOrDefault(d => d.Id == (_settings.SelectedDeviceId ?? ""));
        CmbDevice.SelectedItem = match ?? items[0];
    }

    // --- Appearance tab: sliders ---

    private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtOpacityValue != null)
            TxtOpacityValue.Text = $"{(int)e.NewValue}%";
    }

    private void SliderTranscriptionFont_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtTranscriptionFontValue != null)
            TxtTranscriptionFontValue.Text = ((int)e.NewValue).ToString();
    }

    private void SliderAnswerFont_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtAnswerFontValue != null)
            TxtAnswerFontValue.Text = ((int)e.NewValue).ToString();
    }

    private void SliderLogFont_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtLogFontValue != null)
            TxtLogFontValue.Text = ((int)e.NewValue).ToString();
    }

    // --- Appearance tab: color previews ---

    private void TxtBgColor_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateColorPreview(TxtBgColor.Text, PreviewBgColor);
    }

    private void TxtTranscriptionColor_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateColorPreview(TxtTranscriptionColor.Text, PreviewTranscriptionColor);
    }

    private void TxtAnswerColor_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateColorPreview(TxtAnswerColor.Text, PreviewAnswerColor);
    }

    private void TxtLogColor_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateColorPreview(TxtLogColor.Text, PreviewLogColor);
    }

    private static void UpdateColorPreview(string hex, Border preview)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            preview.Background = new SolidColorBrush(color);
        }
        catch
        {
            // Invalid color string, ignore
        }
    }

    // --- Save / Cancel ---

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // General
        _settings.OpenAiApiKey = TxtApiKey.Text;

        var modeItem = CmbAudioMode.SelectedItem as ComboBoxItem;
        _settings.AudioMode = modeItem?.Tag as string ?? "loopback";

        var deviceItem = CmbDevice.SelectedItem as DeviceItem;
        _settings.SelectedDeviceId = string.IsNullOrEmpty(deviceItem?.Id) ? null : deviceItem.Id;

        var bufferItem = CmbBufferSeconds.SelectedItem as ComboBoxItem;
        if (int.TryParse(bufferItem?.Tag as string, out int secs))
            _settings.BufferSeconds = secs;

        var whisperItem = CmbWhisperModel.SelectedItem as ComboBoxItem;
        _settings.WhisperModel = whisperItem?.Tag as string ?? "whisper-1";

        // Appearance
        _settings.BackgroundColor = TxtBgColor.Text;
        _settings.BackgroundOpacity = (int)SliderOpacity.Value;

        _settings.TranscriptionFontSize = (int)SliderTranscriptionFont.Value;
        _settings.TranscriptionFontColor = TxtTranscriptionColor.Text;

        _settings.AnswerFontSize = (int)SliderAnswerFont.Value;
        _settings.AnswerFontColor = TxtAnswerColor.Text;

        _settings.ShowLog = ChkShowLog.IsChecked == true;
        _settings.LogFontSize = (int)SliderLogFont.Value;
        _settings.LogFontColor = TxtLogColor.Text;

        // Behavior
        _settings.SilenceDetection = ChkSilenceDetection.IsChecked == true;
        _settings.SimulationMode = ChkSimulationMode.IsChecked == true;

        // Prompt
        _settings.SystemPrompt = TxtSystemPrompt.Text;

        _settings.Save();
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
