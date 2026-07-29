using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Chat client decorator that removes the core request options the capability declares the target model
/// does not accept, before delegating to the inner client.
/// </summary>
/// <remarks>
/// <para>
/// Installed by the chat capability bases around the provider's own client, so a declaration made for the
/// profile editor is also the thing enforced on the wire. Before this existed, each provider hand-wrote an
/// equivalent decorator and had to remember to keep it in step with its declaration; three near-identical
/// copies were in the tree, and a new provider that simply forgot to wrap failed silently.
/// </para>
/// <para>
/// Wrapped innermost, beneath the core middleware pipeline, so every caller is covered at once: the chat
/// service, the agent runtime, and anything holding an <see cref="IChatClient"/> directly. Hiding a field
/// in the editor does not stop a profile saved before a model change, or an alias-driven API caller, from
/// arriving here with a value the model rejects.
/// </para>
/// </remarks>
internal sealed class DeclaredSettingsChatClient(
    IChatClient innerClient,
    IAICapability capability,
    string? boundModelId,
    ILogger? logger)
    : DelegatingChatClient(innerClient)
{
    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(messages, Filter(options), cancellationToken);

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(messages, Filter(options), cancellationToken);

    /// <summary>
    /// Returns the options to send, with declared-unsupported values removed. Returns the caller's instance
    /// untouched when there is nothing to remove, so it is never mutated needlessly.
    /// </summary>
    private ChatOptions? Filter(ChatOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        // Cheap exit before consulting the capability: nothing set means nothing to strip.
        if (options.Temperature is null
            && options.TopP is null
            && options.TopK is null
            && options.FrequencyPenalty is null
            && options.PresencePenalty is null)
        {
            return options;
        }

        // The model the request will actually run against. The bound fallback is load-bearing rather than
        // defensive: the agent runtime builds its ChatOptions without a ModelId, so the creation-time model
        // is the only signal on that path.
        var modelId = options.ModelId ?? boundModelId;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return options;
        }

        var declaration = capability.GetSettingsSupport(modelId);
        if (declaration.UnsupportedProfileSettings.Count == 0)
        {
            return options;
        }

        var unsupported = declaration.AsProfileSettingKeys();
        ChatOptions? filtered = null;

        if (options.Temperature is not null && unsupported.Contains(AIProfileSettingKeys.Temperature))
        {
            (filtered ??= options.Clone()).Temperature = null;
        }

        if (options.TopP is not null && unsupported.Contains(AIProfileSettingKeys.TopP))
        {
            (filtered ??= options.Clone()).TopP = null;
        }

        if (options.TopK is not null && unsupported.Contains(AIProfileSettingKeys.TopK))
        {
            (filtered ??= options.Clone()).TopK = null;
        }

        if (options.FrequencyPenalty is not null && unsupported.Contains(AIProfileSettingKeys.FrequencyPenalty))
        {
            (filtered ??= options.Clone()).FrequencyPenalty = null;
        }

        if (options.PresencePenalty is not null && unsupported.Contains(AIProfileSettingKeys.PresencePenalty))
        {
            (filtered ??= options.Clone()).PresencePenalty = null;
        }

        if (filtered is null)
        {
            return options;
        }

        logger?.LogDebug(
            "Model '{ModelId}' declares {Unsupported} unsupported; removed from the request.",
            modelId,
            string.Join(", ", unsupported));

        return filtered;
    }
}
