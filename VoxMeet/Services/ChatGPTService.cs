using OpenAI.Chat;
using VoxMeet.Models;

namespace VoxMeet.Services;

public class ChatGPTService
{
    private ChatClient? _chatClient;
    private string _systemPrompt = "";

    public event Action<string>? AnswerReceived;
    public event Action<Exception>? ErrorOccurred;

    public void Configure(string apiKey)
    {
        _chatClient = new ChatClient("gpt-4o-mini", apiKey);
        _systemPrompt = AppSettings.Load().SystemPrompt;
    }

    public async Task AskAsync(string question, CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            ErrorOccurred?.Invoke(new InvalidOperationException("API key not configured."));
            return;
        }

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(_systemPrompt),
                new UserChatMessage(question)
            };

            var completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

            var answer = completion.Value.Content[0].Text?.Trim();
            if (!string.IsNullOrEmpty(answer))
            {
                AnswerReceived?.Invoke(answer);
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
}
