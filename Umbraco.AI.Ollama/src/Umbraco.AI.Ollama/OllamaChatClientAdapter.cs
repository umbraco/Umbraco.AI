using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using AIChatRole = Microsoft.Extensions.AI.ChatRole;
using OllamaChatRole = OllamaSharp.Models.Chat.ChatRole;

namespace Umbraco.AI.Ollama;

/// <summary>
/// Adapter that wraps OllamaApiClient to implement IChatClient from Microsoft.Extensions.AI.
/// </summary>
internal sealed class OllamaChatClientAdapter : IChatClient
{
    private readonly OllamaApiClient _ollamaClient;

    public OllamaChatClientAdapter(OllamaApiClient ollamaClient)
    {
        _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
    }

    public ChatClientMetadata Metadata => new("Ollama", _ollamaClient.Uri, _ollamaClient.SelectedModel);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest
        {
            Model = _ollamaClient.SelectedModel,
            Messages = ConvertMessages(chatMessages),
            Stream = false
        };

        ApplyOptions(request, options);

        ChatResponseStream? finalResponse = null;
        var contentBuilder = new System.Text.StringBuilder();

        await foreach (var response in _ollamaClient.ChatAsync(request, cancellationToken))
        {
            if (response?.Message?.Content is not null)
            {
                contentBuilder.Append(response.Message.Content);
            }

            if (response?.Done == true)
            {
                finalResponse = response;
                break;
            }
        }

        // Try to extract usage information if available
        UsageDetails? usage = null;
        if (finalResponse != null)
        {
            // ChatResponseStream may have different property names for token counts
            // We'll leave usage as null for now - can be enhanced once property names are confirmed
            usage = new UsageDetails();
        }

        return new ChatResponse(new ChatMessage(AIChatRole.Assistant, contentBuilder.ToString()))
        {
            ModelId = finalResponse?.Model ?? _ollamaClient.SelectedModel,
            FinishReason = ChatFinishReason.Stop,
            Usage = usage
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest
        {
            Model = _ollamaClient.SelectedModel,
            Messages = ConvertMessages(chatMessages),
            Stream = true
        };

        ApplyOptions(request, options);

        await foreach (var response in _ollamaClient.ChatAsync(request, cancellationToken))
        {
            if (response?.Message?.Content is not null)
            {
                yield return new ChatResponseUpdate(AIChatRole.Assistant, response.Message.Content);
            }

            if (response?.Done == true)
            {
                yield break;
            }
        }
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        if (serviceType == typeof(IChatClient) || serviceType == typeof(OllamaChatClientAdapter))
        {
            return this;
        }

        return null;
    }

    public void Dispose()
    {
        // OllamaApiClient doesn't implement IDisposable
        // If needed in the future, we can dispose resources here
    }

    private static List<Message> ConvertMessages(IEnumerable<ChatMessage> chatMessages)
    {
        var messages = new List<Message>();

        foreach (var message in chatMessages)
        {
            var role = message.Role.Value switch
            {
                "user" => OllamaChatRole.User,
                "assistant" => OllamaChatRole.Assistant,
                "system" => OllamaChatRole.System,
                _ => OllamaChatRole.User
            };

            var content = string.Empty;
            foreach (var item in message.Contents)
            {
                if (item is TextContent textContent)
                {
                    content += textContent.Text;
                }
            }

            messages.Add(new Message(role, content, null));
        }

        return messages;
    }

    private static void ApplyOptions(ChatRequest request, ChatOptions? options)
    {
        if (options is null)
        {
            return;
        }

        if (options.Temperature.HasValue)
        {
            request.Options ??= new RequestOptions();
            request.Options.Temperature = (float)options.Temperature.Value;
        }

        if (options.TopP.HasValue)
        {
            request.Options ??= new RequestOptions();
            request.Options.TopP = (float)options.TopP.Value;
        }

        if (options.MaxOutputTokens.HasValue)
        {
            request.Options ??= new RequestOptions();
            request.Options.NumPredict = options.MaxOutputTokens.Value;
        }

        if (options.StopSequences is { Count: > 0 })
        {
            request.Options ??= new RequestOptions();
            request.Options.Stop = [.. options.StopSequences];
        }
    }
}
