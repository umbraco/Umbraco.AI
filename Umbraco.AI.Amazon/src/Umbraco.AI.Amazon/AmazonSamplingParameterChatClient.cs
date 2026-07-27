using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Amazon;

/// <summary>
/// A chat client decorator that removes the sampling parameters (<c>Temperature</c>, <c>TopP</c>,
/// <c>TopK</c>) from the request when the target Bedrock model does not accept them.
/// </summary>
/// <remarks>
/// <para>
/// Bedrock fronts other vendors' models, so it inherits their restrictions: a Bedrock-hosted
/// <c>anthropic.claude-opus-4-8</c> rejects the sampling parameters exactly as the first-party Anthropic
/// model does. Filtering here — inside the provider, beneath the core middleware pipeline — covers every
/// caller at once: the chat service (<c>AIChatService.MergeOptions</c>), the agent runtime
/// (<c>AIAgentFactory</c>), and anything calling <c>IChatClient</c> directly.
/// </para>
/// <para>
/// The model is resolved from <see cref="ChatOptions.ModelId"/> when the caller set one, falling back to
/// the model the client was bound to at creation. The fallback is load-bearing rather than defensive: the
/// agent runtime builds its <see cref="ChatOptions"/> without a <c>ModelId</c>, so the bound value is the
/// only way to identify the model on that path.
/// </para>
/// <para>
/// The caller's <see cref="ChatOptions"/> is never mutated; a clone is taken only when something actually
/// needs removing, so the common case allocates nothing.
/// </para>
/// </remarks>
internal sealed class AmazonSamplingParameterChatClient(
    IChatClient innerClient,
    string? boundModelId,
    ILogger? logger)
    : DelegatingChatClient(innerClient)
{
    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(messages, FilterOptions(options), cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(messages, FilterOptions(options), cancellationToken);

    /// <summary>
    /// Returns the options to send, with the sampling parameters removed when the resolved model
    /// does not accept them. Returns the original instance untouched when there is nothing to remove.
    /// </summary>
    internal ChatOptions? FilterOptions(ChatOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        if (options.Temperature is null && options.TopP is null && options.TopK is null)
        {
            return options;
        }

        var modelId = options.ModelId ?? boundModelId;
        if (AmazonModelUtilities.SupportsSamplingParameters(modelId))
        {
            return options;
        }

        var filtered = options.Clone();
        filtered.Temperature = null;
        filtered.TopP = null;
        filtered.TopK = null;

        logger?.LogDebug(
            "Bedrock model '{ModelId}' does not accept the sampling parameters; " +
            "Temperature/TopP/TopK were removed from the request.",
            modelId ?? "(unresolved)");

        return filtered;
    }
}
