using System.Runtime.CompilerServices;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// Chat client decorator that reports how much of a response's input was served from Anthropic's prompt
/// cache, under <see cref="Constants.UsageCounts.CachedInputTokens"/>.
/// </summary>
/// <remarks>
/// <para>
/// Anthropic returns the input total split three ways — fresh, written to cache, and read from cache — but
/// the SDK's Microsoft.Extensions.AI adapter sums all three into
/// <see cref="UsageDetails.InputTokenCount"/> and forwards only the cache <em>write</em> count in
/// <see cref="UsageDetails.AdditionalCounts"/>. The read count, which is the one that shows caching paying
/// off, is dropped. This recovers it from the underlying Anthropic response so the analytics and audit
/// layers can persist it.
/// </para>
/// <para>
/// Only the reported count is added; <see cref="UsageDetails.InputTokenCount"/> is left alone because it
/// is already the true total, of which this is a subset.
/// </para>
/// </remarks>
internal sealed class AnthropicCachedTokenReportingChatClient(IChatClient innerClient)
    : DelegatingChatClient(innerClient)
{
    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);

        if (response.RawRepresentation is BetaMessage message)
        {
            Report(response.Usage, message.Usage.CacheReadInputTokens);
        }

        return response;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The streamed usage update carries no raw representation of its own, so the count is taken from the
    /// <c>message_delta</c> event that precedes it and applied when the usage arrives. Tracked across the
    /// whole stream rather than per update because the two are separate items in the sequence.
    /// </remarks>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        long? cachedInputTokens = null;

        var updates = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .WithCancellation(cancellationToken);

        await foreach (var update in updates.ConfigureAwait(false))
        {
            if (update.RawRepresentation is BetaRawMessageStreamEvent streamEvent
                && streamEvent.TryPickDelta(out var delta)
                && delta.Usage.CacheReadInputTokens is { } reported)
            {
                cachedInputTokens = reported;
            }

            foreach (var usage in update.Contents.OfType<UsageContent>())
            {
                Report(usage.Details, cachedInputTokens);
            }

            yield return update;
        }
    }

    /// <summary>
    /// Records the cache-read count on the usage details, when there is both a count and somewhere to put
    /// it.
    /// </summary>
    private static void Report(UsageDetails? usage, long? cachedInputTokens)
    {
        if (usage is null || cachedInputTokens is null)
        {
            return;
        }

        usage.AdditionalCounts ??= [];
        usage.AdditionalCounts[Constants.UsageCounts.CachedInputTokens] = cachedInputTokens.Value;
    }
}
