using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.InlineChat;

/// <summary>
/// A chat client decorator that creates a runtime context scope per-execution
/// and sets inline-chat metadata in the runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="GetResponseAsync"/> or <see cref="GetStreamingResponseAsync"/>
/// creates a fresh scope, populates it via contributors, sets inline-chat feature metadata,
/// delegates to the inner client, and disposes the scope. This mirrors the <c>ScopedAIAgent</c>
/// pattern from the Agent package.
/// </para>
/// <para>
/// This client is returned by <see cref="IAIChatService.CreateInlineChatClientAsync"/> and
/// does not publish notifications (matching <c>CreateInlineAgentAsync</c> behavior).
/// </para>
/// </remarks>
internal sealed class ScopedInlineChatClient : DelegatingChatClient
{
    private readonly AIInlineChatBuilder _builder;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedInlineChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The base chat client to delegate to.</param>
    /// <param name="builder">The inline chat builder containing configuration.</param>
    /// <param name="scopeProvider">Provider for creating runtime context scopes.</param>
    /// <param name="contributors">Collection of context contributors to populate the scope.</param>
    internal ScopedInlineChatClient(
        IChatClient innerClient,
        AIInlineChatBuilder builder,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
        : base(innerClient)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeProvider.CreateScope(_builder.ContextItems ?? []);
        PopulateScopeContext(scope.Context);
        return await base.GetResponseAsync(messages, options, cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = _scopeProvider.CreateScope(_builder.ContextItems ?? []);
        PopulateScopeContext(scope.Context);

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// Populates the runtime context scope with contributors and sets inline-chat metadata.
    /// </summary>
    private void PopulateScopeContext(AIRuntimeContext context)
    {
        _contributors.Populate(context);

        context.SetValue(Constants.ContextKeys.FeatureType, "inline-chat");
        context.SetValue(Constants.ContextKeys.FeatureId, _builder.Id);
        context.SetValue(Constants.ContextKeys.FeatureAlias, _builder.Alias);

        // Set guardrail IDs override if specified
        if (_builder.GuardrailIds.Count > 0)
        {
            context.SetValue(Constants.ContextKeys.GuardrailIdsOverride, _builder.GuardrailIds);
        }

        // Set ChatOptions override if specified
        if (_builder.ChatOptions is not null)
        {
            context.SetValue(Constants.ContextKeys.ChatOptionsOverride, _builder.ChatOptions);
        }

        // Set additional properties
        if (_builder.AdditionalProperties is not null)
        {
            foreach (var property in _builder.AdditionalProperties)
            {
                context.SetValue(property.Key, property.Value);
            }
        }
    }
}
