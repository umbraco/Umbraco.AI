using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.InlineChat;

/// <summary>
/// A chat client decorator that manages runtime context scope per-execution
/// and sets inline-chat metadata in the runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="GetResponseAsync"/> or <see cref="GetStreamingResponseAsync"/>
/// ensures a scope exists, populates it via contributors if newly created, sets inline-chat
/// feature metadata (only when no parent scope already set it), delegates to the inner client,
/// and disposes any scope it created. This mirrors the <c>ScopedProfileChatClient</c> pattern.
/// </para>
/// <para>
/// This client is returned by <see cref="IAIChatService.CreateInlineChatClientAsync"/> and
/// does not publish notifications (matching <c>CreateInlineAgentAsync</c> behavior).
/// </para>
/// </remarks>
internal sealed class ScopedInlineChatClient : DelegatingChatClient
{
    private readonly AIInlineChatBuilder _builder;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedInlineChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The base chat client to delegate to.</param>
    /// <param name="builder">The inline chat builder containing configuration.</param>
    /// <param name="contextAccessor">Accessor for the runtime context.</param>
    /// <param name="scopeProvider">Provider for creating runtime context scopes.</param>
    /// <param name="contributors">Collection of context contributors to populate the scope.</param>
    internal ScopedInlineChatClient(
        IChatClient innerClient,
        AIInlineChatBuilder builder,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
        : base(innerClient)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(_builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }

            PopulateScopeContext(_contextAccessor.Context!, scopeExisted);
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(_builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }

            PopulateScopeContext(_contextAccessor.Context!, scopeExisted);

            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                yield return update;
            }
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    /// <summary>
    /// Populates the runtime context with inline-chat metadata.
    /// </summary>
    private void PopulateScopeContext(AIRuntimeContext context, bool scopeExisted)
    {
        // Only set feature metadata when we created the scope ourselves.
        // When a parent scope exists, it already has its own feature identity.
        if (!scopeExisted)
        {
            context.SetValue(Constants.ContextKeys.FeatureType, "inline-chat");
            context.SetValue(Constants.ContextKeys.FeatureId, _builder.Id);
            context.SetValue(Constants.ContextKeys.FeatureAlias, _builder.Alias);
        }

        // These are always set — they don't conflict with parent scope metadata
        if (_builder.GuardrailIds.Count > 0)
        {
            context.SetValue(Constants.ContextKeys.GuardrailIdsOverride, _builder.GuardrailIds);
        }

        if (_builder.ChatOptions is not null)
        {
            context.SetValue(Constants.ContextKeys.ChatOptionsOverride, _builder.ChatOptions);
        }

        if (_builder.AdditionalProperties is not null)
        {
            foreach (var property in _builder.AdditionalProperties)
            {
                context.SetValue(property.Key, property.Value);
            }
        }
    }
}
