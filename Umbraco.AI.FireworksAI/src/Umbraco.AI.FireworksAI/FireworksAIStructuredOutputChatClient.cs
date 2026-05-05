using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.FireworksAI;

/// <summary>
/// Drops <see cref="ChatOptions.Tools"/> when a <see cref="ChatOptions.ResponseFormat"/> is set.
/// </summary>
/// <remarks>
/// Fireworks AI rejects requests that combine a response_format with function/tool definitions
/// ("You cannot specify response format and function call at the same time"). When the caller
/// asks for structured output, we honour that and discard the tools — the prompt feature attaches
/// helper tools by default, but structured output is what the caller actually needs to parse.
/// </remarks>
internal sealed class FireworksAIStructuredOutputChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(messages, Sanitize(options), cancellationToken);

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, Sanitize(options), cancellationToken))
        {
            yield return update;
        }
    }

    private static ChatOptions? Sanitize(ChatOptions? options)
    {
        if (options?.ResponseFormat is null || options.Tools is not { Count: > 0 })
        {
            return options;
        }

        var clone = options.Clone();
        clone.Tools = null;
        clone.ToolMode = null;
        return clone;
    }
}
