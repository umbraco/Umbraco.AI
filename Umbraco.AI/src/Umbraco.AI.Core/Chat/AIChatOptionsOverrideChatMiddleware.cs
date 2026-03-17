using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// Chat middleware that reads <see cref="Constants.ContextKeys.ChatOptionsOverride"/> from the
/// runtime context and merges those options into the call's options (override values take precedence).
/// </summary>
/// <remarks>
/// This middleware enables inline agent and inline chat builders to pass ChatOptions through
/// the middleware pipeline without requiring changes to the ScopedProfileChatClient.
/// </remarks>
internal sealed class AIChatOptionsOverrideChatMiddleware : IAIChatMiddleware
{
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AIChatOptionsOverrideChatMiddleware(IAIRuntimeContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public IChatClient Apply(IChatClient client)
    {
        return new AIChatOptionsOverrideChatClient(client, _contextAccessor);
    }
}

/// <summary>
/// Chat client decorator that applies ChatOptions overrides from the runtime context.
/// </summary>
internal sealed class AIChatOptionsOverrideChatClient : DelegatingChatClient
{
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AIChatOptionsOverrideChatClient(
        IChatClient innerClient,
        IAIRuntimeContextAccessor contextAccessor)
        : base(innerClient)
    {
        _contextAccessor = contextAccessor;
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options = ApplyOverrides(options);
        return base.GetResponseAsync(messages, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options = ApplyOverrides(options);
        return base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    private ChatOptions? ApplyOverrides(ChatOptions? options)
    {
        var context = _contextAccessor.Context;
        if (context is null)
        {
            return options;
        }

        var overrideOptions = context.GetValue<ChatOptions>(Constants.ContextKeys.ChatOptionsOverride);
        if (overrideOptions is null)
        {
            return options;
        }

        // If no existing options, use overrides directly
        if (options is null)
        {
            return overrideOptions;
        }

        // Merge: override values take precedence over existing options
        return new ChatOptions
        {
            ModelId = overrideOptions.ModelId ?? options.ModelId,
            Temperature = overrideOptions.Temperature ?? options.Temperature,
            MaxOutputTokens = overrideOptions.MaxOutputTokens ?? options.MaxOutputTokens,
            TopP = overrideOptions.TopP ?? options.TopP,
            FrequencyPenalty = overrideOptions.FrequencyPenalty ?? options.FrequencyPenalty,
            PresencePenalty = overrideOptions.PresencePenalty ?? options.PresencePenalty,
            StopSequences = overrideOptions.StopSequences ?? options.StopSequences,
            ResponseFormat = overrideOptions.ResponseFormat ?? options.ResponseFormat,
            Tools = overrideOptions.Tools ?? options.Tools,
            ToolMode = overrideOptions.ToolMode ?? options.ToolMode,
            AdditionalProperties = overrideOptions.AdditionalProperties ?? options.AdditionalProperties
        };
    }
}
