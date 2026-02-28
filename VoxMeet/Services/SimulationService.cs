namespace VoxMeet.Services;

public class SimulationService
{
    private static readonly string[] Questions =
    [
        "What is the difference between SQL and NoSQL databases?",
        "Can you explain what RESTful APIs are and how they work?",
        "What is the purpose of version control systems like Git?",
        "How does the client-server architecture work?",
        "What are the main principles of object-oriented programming?",
        "Can you explain the concept of CI/CD pipelines?",
        "What is the difference between authentication and authorization?",
        "How do you approach debugging a production issue?",
        "What are design patterns and why are they useful?",
        "Can you explain how caching improves application performance?"
    ];

    private CancellationTokenSource? _cts;

    public void Start(Action<string> appendLog, Action<string> appendTranscription, Action<string> appendAnswer)
    {
        Stop();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            appendLog("[SIMULATION] This is a simulation. No audio capture or AI connection.");
            appendLog("Recording started. Waiting for audio...");

            while (!token.IsCancellationRequested)
            {
                var question = Questions[Random.Shared.Next(Questions.Length)];

                await Task.Delay(3000, token);

                appendLog("Audio chunk captured (0.48 MB). Sending to API...");

                await Task.Delay(1000, token);

                appendLog("Sending audio to Whisper API (0.48 MB)...");

                await Task.Delay(2000, token);

                appendLog("Waiting for Whisper API response...");

                await Task.Delay(2000, token);

                appendLog("Transcription received.");
                appendTranscription(question);

                await Task.Delay(1000, token);

                appendLog($"Question detected: \"{question}\"");
                appendLog("Sending question to ChatGPT...");

                await Task.Delay(3000, token);

                appendLog("ChatGPT answer received.");
                appendAnswer("[Simulated answer for testing purposes]");

                await Task.Delay(2000, token);

                appendLog("Waiting for audio...");
            }
        }, token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
