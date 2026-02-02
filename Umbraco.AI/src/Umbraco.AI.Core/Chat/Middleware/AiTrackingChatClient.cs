using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Chat.Middleware;

/// <summary>
/// A chat client that tracks the last usage details and response message.
/// </summary>
/// <param name="innerClient">The inner chat client to wrap.</param>
internal sealed class AiTrackingChatClient(IChatClient innerClient) : AiBoundChatClientBase(innerClient)
{
    /// <summary>
    /// The last usage details received from the chat client.
    /// </summary>
    public UsageDetails? LastUsageDetails { get; private set; }

    /// <summary>
    /// The response messages received from the chat client.
    /// This includes all messages after the user's request (assistant messages, tool results, etc.)
    /// for complete audit logging of tool-use scenarios.
    /// </summary>
    public IReadOnlyList<ChatMessage>? LastResponseMessages { get; private set; }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);

        LastUsageDetails = response.Usage;
        // Capture all response messages for complete audit logging (includes tool calls and results in agentic scenarios)
        LastResponseMessages = response.Messages.ToList();

        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stream = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken);

        // Accumulate updates while yielding immediately
        var updates = new List<ChatResponseUpdate>();

        // Stream updates - yield immediately, then accumulate for later
        await foreach (var update in stream)
        {
            yield return update;  // IMMEDIATE yield - no blocking
            updates.Add(update);  // Accumulate for post-stream aggregation
        }

        // After streaming completes, aggregate for audit logging using M.E.AI's ToChatResponse()
        // This handles text content concatenation, function call assembly, and usage accumulation
        var aggregatedResponse = updates.ToChatResponse();
        LastUsageDetails = aggregatedResponse.Usage;
        // Capture all response messages for complete audit logging (includes tool calls and results in agentic scenarios)
        LastResponseMessages = aggregatedResponse.Messages.ToList();
    }
}