using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// Adds a block-level Anthropic <c>cache_control</c> marker to the first system-role message, so the
/// stable system+tools prefix earns its own cache breakpoint independent of the request's tail.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AnthropicChatCapability"/>'s top-level <c>cache_control</c> field marks the last cacheable
/// block in the whole request, and Anthropic's cache lookup walks back only roughly 20 content blocks from
/// that single breakpoint. In a longer conversation with tool calls, that window never reaches back to the
/// system prompt, so the stable prefix (agent instructions, context resources -- see
/// <c>AIAgentSystemMessageChatClient</c> in Umbraco.AI.Agent.Core, which always folds that content into the
/// message list's leading entry, then appends the volatile runtime-context prompt as its own message at the
/// end) never gets a breakpoint of its own. This client adds a second, block-level marker directly on that
/// stable content -- the FIRST system-role message, which by construction is always the stable one, however
/// many other system-role messages (e.g. the volatile trailing one) follow it.
/// </para>
/// <para>
/// Verified against the Anthropic SDK's own message-to-request conversion (see
/// <c>AnthropicPromptCachingWireTests</c>): a system-role <see cref="ChatMessage"/>'s
/// <see cref="TextContent"/>, when its <see cref="AIContent.RawRepresentation"/> is set to a
/// <see cref="BetaTextBlockParam"/>, is honoured verbatim on the wire in place of the plain block the
/// adapter would otherwise derive from <see cref="TextContent.Text"/>. This is the only reachable way to
/// attach <c>cache_control</c> to system content the adapter assembles from the message list --
/// <see cref="ChatOptions.RawRepresentationFactory"/> only supplies a base template the adapter merges
/// into, with no view of the actual message content, which is why
/// <see cref="AnthropicChatCapability.ApplyCapabilitySettings"/> cannot place a marker here itself.
/// </para>
/// <para>
/// Builds new message/content instances rather than mutating the ones passed in: a caller may reuse the
/// same <see cref="ChatMessage"/>/<see cref="AIContent"/> objects across requests (e.g. as stored
/// conversation history), and mutating a shared object's <see cref="AIContent.RawRepresentation"/> in place
/// would leak this marker into an unrelated later request that reuses it.
/// </para>
/// </remarks>
internal sealed class AnthropicSystemBlockCacheMarkingChatClient : DelegatingChatClient
{
    /// <summary>
    /// Key under which <see cref="AnthropicChatCapability.ApplyCapabilitySettings"/> stashes this request's
    /// resolved <see cref="BetaCacheControlEphemeral"/> on <see cref="ChatOptions.AdditionalProperties"/>, so
    /// this client can reuse the same decision without re-resolving it from the stored TTL setting. Never
    /// reaches the wire -- <see cref="ChatOptions.AdditionalProperties"/> is dropped by the SDK's adapter
    /// before the request is built.
    /// </summary>
    internal const string CacheControlPropertyKey = "__umbraco_ai_anthropic_system_block_cache_control";

    public AnthropicSystemBlockCacheMarkingChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(MarkFirstSystemMessage(messages, options), options, cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(MarkFirstSystemMessage(messages, options), options, cancellationToken);

    private static IEnumerable<ChatMessage> MarkFirstSystemMessage(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        if (options?.AdditionalProperties?.TryGetValue(CacheControlPropertyKey, out var raw) != true
            || raw is not BetaCacheControlEphemeral cacheControl)
        {
            return messages;
        }

        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

        var firstSystemIndex = -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Role == ChatRole.System)
            {
                firstSystemIndex = i;
                break;
            }
        }

        if (firstSystemIndex < 0)
        {
            return list;
        }

        var target = list[firstSystemIndex];
        var lastText = target.Contents.OfType<TextContent>().LastOrDefault();
        if (lastText is null)
        {
            return list;
        }

        var markedContents = target.Contents
            .Select(content => content == lastText
                ? new TextContent(lastText.Text)
                {
                    RawRepresentation = new BetaTextBlockParam { Text = lastText.Text, CacheControl = cacheControl },
                }
                : content)
            .ToList();

        var marked = target.Clone();
        marked.Contents = markedContents;

        var result = list.ToList();
        result[firstSystemIndex] = marked;
        return result;
    }
}
