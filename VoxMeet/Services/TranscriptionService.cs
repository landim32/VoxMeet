using System.IO;
using OpenAI.Audio;

namespace VoxMeet.Services;

public class TranscriptionService : IDisposable
{
    private AudioClient? _audioClient;

    public event Action<string>? StatusChanged;
    public event Action<string>? TranscriptionReceived;
    public event Action<Exception>? ErrorOccurred;

    public void Configure(string apiKey)
    {
        _audioClient = new AudioClient("whisper-1", apiKey);
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
            if (!string.IsNullOrEmpty(text))
            {
                TranscriptionReceived?.Invoke(text);
            }
            else
            {
                StatusChanged?.Invoke("No speech detected in this chunk.");
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

    public void Dispose()
    {
        // AudioClient doesn't implement IDisposable
    }
}
