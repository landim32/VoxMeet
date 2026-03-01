using System.IO;
using OpenAI.Audio;

namespace VoxMeet.Services;

public class TranscriptionService : IDisposable
{
    private AudioClient? _audioClient;

    public event Action<string>? StatusChanged;
    public event Action<string>? TranscriptionReceived;
    public event Action? SilenceDetected;
    public event Action<Exception>? ErrorOccurred;

    public void Configure(string apiKey, string model = "whisper-1")
    {
        _audioClient = new AudioClient(model, apiKey);
    }

    public async Task TranscribeChunkAsync(byte[] wavData, CancellationToken cancellationToken = default)
    {
        if (_audioClient == null)
        {
            ErrorOccurred?.Invoke(new InvalidOperationException("API key not configured."));
            return;
        }

        try
        {
            var sizeMb = wavData.Length / 1024.0 / 1024.0;
            StatusChanged?.Invoke($"Sending audio to Whisper API ({sizeMb:F2} MB)...");

            using var stream = new MemoryStream(wavData);

            StatusChanged?.Invoke("Waiting for Whisper API response...");

            var result = await _audioClient.TranscribeAudioAsync(
                stream,
                "chunk.wav",
                null,
                cancellationToken);

            var text = result.Value.Text?.Trim();
            if (!string.IsNullOrEmpty(text) && !IsWhisperHallucination(text))
            {
                TranscriptionReceived?.Invoke(text);
            }
            else
            {
                StatusChanged?.Invoke("No speech detected in this chunk.");
                SilenceDetected?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }

    private static readonly HashSet<string> HallucinationPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "you", "thank you", "thanks", "bye", "goodbye",
        "thanks for watching", "thank you for watching",
        "subscribe", "like and subscribe",
        "the end", "so", "yeah", "okay", "ok",
        "um", "uh", "hmm", "huh", "ah",
        "...", ".", "!", "?",
    };

    private static bool IsWhisperHallucination(string text)
    {
        var cleaned = text.Trim('.', '!', '?', ' ', ',');
        return cleaned.Length < 3 || HallucinationPhrases.Contains(cleaned);
    }

    public void Dispose()
    {
        // AudioClient doesn't implement IDisposable
    }
}
