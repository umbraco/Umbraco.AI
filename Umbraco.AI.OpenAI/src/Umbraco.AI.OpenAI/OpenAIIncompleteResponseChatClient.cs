using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Reports why a streamed Responses API response was cut short, which the Microsoft.Extensions.AI adapter
/// leaves out.
/// </summary>
/// <remarks>
/// The adapter turns the <c>response.incomplete</c> event into an update with no
/// <see cref="ChatResponseUpdate.FinishReason"/>, so a response stopped at <c>max_output_tokens</c> is
/// indistinguishable from one that finished normally. The agent runtime relies on
/// <see cref="ChatFinishReason.Length"/> to tell the user why a run stopped (#414).
/// </remarks>
[Experimental("OPENAI001")]
internal sealed class OpenAIIncompleteResponseChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.FinishReason is null
                && update.RawRepresentation is StreamingResponseIncompleteUpdate incomplete
                && ToFinishReason(incomplete.Response?.IncompleteStatusDetails?.Reason) is { } finishReason)
            {
                update.FinishReason = finishReason;
            }

            yield return update;
        }
    }

    private static ChatFinishReason? ToFinishReason(ResponseIncompleteStatusReason? reason)
    {
        if (reason == ResponseIncompleteStatusReason.MaxOutputTokens)
        {
            return ChatFinishReason.Length;
        }

        if (reason == ResponseIncompleteStatusReason.ContentFilter)
        {
            return ChatFinishReason.ContentFilter;
        }

        return null;
    }
}
