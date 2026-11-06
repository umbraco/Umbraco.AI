using Microsoft.Extensions.AI;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// Moves every system-role message to the front of the message list, so they all end up in the request's
/// top-level <c>system</c> prompt.
/// </summary>
/// <remarks>
/// The SDK's Microsoft.Extensions.AI adapter used to fold a system-role message anywhere in the list into the
/// top-level <c>system</c> prompt. Since API version 2026-10-21 it emits one that follows other messages as a
/// <c>{ "role": "system" }</c> entry in <c>messages</c> instead, which the API rejects for models and
/// placements that do not support it. The agent runtime appends its volatile runtime-context prompt as a
/// trailing system message, so without this every such request would depend on that support. Hoisting keeps
/// the earlier wire shape. Builds a new list; the caller's messages are not touched.
/// </remarks>
internal sealed class AnthropicSystemMessageHoistingChatClient : DelegatingChatClient
{
    public AnthropicSystemMessageHoistingChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(HoistSystemMessages(messages), options, cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(HoistSystemMessages(messages), options, cancellationToken);

    private static IEnumerable<ChatMessage> HoistSystemMessages(IEnumerable<ChatMessage> messages)
    {
        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

        // Already in order when no system message follows a non-system one.
        var seenOther = false;
        var needsHoist = false;
        foreach (var message in list)
        {
            if (message.Role != ChatRole.System)
            {
                seenOther = true;
            }
            else if (seenOther)
            {
                needsHoist = true;
                break;
            }
        }

        if (!needsHoist)
        {
            return list;
        }

        return list.Where(m => m.Role == ChatRole.System)
            .Concat(list.Where(m => m.Role != ChatRole.System))
            .ToList();
    }
}
