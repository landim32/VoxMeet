using System.IO;
using System.Text.Json;

namespace VoxMeet.Models;

public class AppSettings
{
    // General
    public string? OpenAiApiKey { get; set; }
    public string AudioMode { get; set; } = "loopback";
    public string? SelectedDeviceId { get; set; }
    public int BufferSeconds { get; set; } = 5;

    // Appearance
    public int BackgroundOpacity { get; set; } = 80;
    public string BackgroundColor { get; set; } = "#1E1E2E";

    // Transcription (Question)
    public int TranscriptionFontSize { get; set; } = 17;
    public string TranscriptionFontColor { get; set; } = "#EEEEF0";

    // Answer
    public int AnswerFontSize { get; set; } = 19;
    public string AnswerFontColor { get; set; } = "#94E2D5";

    // Prompt
    public string SystemPrompt { get; set; } = """
        You are a helpful assistant in a real-time technical interview/meeting scenario.
        All questions will be technical, related to software development and programming.
        Answer questions as simply and concisely as possible.
        Use short, direct answers. Avoid unnecessary explanations.
        Always answer in the same language as the question.
        """;

    // Log
    public bool ShowLog { get; set; } = true;
    public int LogFontSize { get; set; } = 11;
    public string LogFontColor { get; set; } = "#6C7086";

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VoxMeet");

    private static readonly string SettingsFile =
        Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsFile))
            return new AppSettings();

        var json = File.ReadAllText(SettingsFile);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFile, json);
    }
}
