using System.ClientModel;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Perplexity;

/// <summary>
/// Delegating chat client that adapts Umbraco.AI's chat pipeline to Perplexity's
/// stricter Sonar API: moves system messages to the front of the conversation
/// (Perplexity requires the last message to be `user` or `tool`) and surfaces
/// 4xx response bodies in exceptions so configuration mistakes are debuggable.
/// </summary>
internal sealed class PerplexityChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(NormalizeMessages(messages), StripUnsupportedOptions(options), cancellationToken);
        }
        catch (ClientResultException ex)
        {
            throw EnrichException(ex);
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<ChatResponseUpdate> enumerator;
        try
        {
            enumerator = base.GetStreamingResponseAsync(NormalizeMessages(messages), StripUnsupportedOptions(options), cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (ClientResultException ex)
        {
            throw EnrichException(ex);
        }

        try
        {
            while (true)
            {
                ChatResponseUpdate update;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        yield break;
                    }
                    update = enumerator.Current;
                }
                catch (ClientResultException ex)
                {
                    throw EnrichException(ex);
                }

                yield return update;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    /// <summary>
    /// Removes options Perplexity's Sonar models reject. Sonar doesn't support
    /// tool/function calling, so any `Tools` or `ToolMode` in the request will
    /// trigger a 400 from the API.
    /// </summary>
    private static ChatOptions? StripUnsupportedOptions(ChatOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        if ((options.Tools is null || options.Tools.Count == 0) && options.ToolMode is null)
        {
            return options;
        }

        var clone = options.Clone();
        clone.Tools = null;
        clone.ToolMode = null;
        return clone;
    }

    /// <summary>
    /// Reorders messages so any system-role messages come first, preserving the
    /// relative order of all other messages. Perplexity rejects requests where
    /// the last message has role other than `user` or `tool`, and Umbraco.AI's
    /// middleware pipeline can append a system message after the user prompt
    /// (e.g., for runtime context injection).
    /// </summary>
    private static IEnumerable<ChatMessage> NormalizeMessages(IEnumerable<ChatMessage> messages)
    {
        var materialized = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        var systemMessages = materialized.Where(m => m.Role == ChatRole.System);
        var others = materialized.Where(m => m.Role != ChatRole.System);
        return systemMessages.Concat(others);
    }

    private static InvalidOperationException EnrichException(ClientResultException ex)
    {
        string? body = null;
        try
        {
            body = ex.GetRawResponse()?.Content?.ToString();
        }
        catch
        {
            // best-effort — fall back to the original message
        }

        var message = string.IsNullOrWhiteSpace(body)
            ? $"Perplexity API error (status {ex.Status}): {ex.Message}"
            : $"Perplexity API error (status {ex.Status}): {body}";

        return new InvalidOperationException(message, ex);
    }
}
