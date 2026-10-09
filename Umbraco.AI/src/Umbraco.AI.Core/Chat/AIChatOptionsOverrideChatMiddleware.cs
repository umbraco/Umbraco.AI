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

        // Copy so neither the caller's options nor the shared override are changed between calls.
        if (options is null)
        {
            return overrideOptions.Clone();
        }

        // Start from a copy of the caller's options so everything the override doesn't set is kept,
        // notably ConversationId: the function-invoking client uses it to link a tool result to the
        // previous response, and dropping it makes the provider reject the follow-up call.
        var merged = options.Clone();
        merged.ModelId = overrideOptions.ModelId ?? options.ModelId;
        merged.Temperature = overrideOptions.Temperature ?? options.Temperature;
        merged.MaxOutputTokens = overrideOptions.MaxOutputTokens ?? options.MaxOutputTokens;
        merged.TopP = overrideOptions.TopP ?? options.TopP;
        merged.FrequencyPenalty = overrideOptions.FrequencyPenalty ?? options.FrequencyPenalty;
        merged.PresencePenalty = overrideOptions.PresencePenalty ?? options.PresencePenalty;
        merged.StopSequences = overrideOptions.StopSequences ?? options.StopSequences;
        merged.ResponseFormat = overrideOptions.ResponseFormat ?? options.ResponseFormat;
        merged.Tools = overrideOptions.Tools ?? options.Tools;
        merged.ToolMode = overrideOptions.ToolMode ?? options.ToolMode;
        merged.AdditionalProperties = overrideOptions.AdditionalProperties ?? options.AdditionalProperties;
        return merged;
    }
}
