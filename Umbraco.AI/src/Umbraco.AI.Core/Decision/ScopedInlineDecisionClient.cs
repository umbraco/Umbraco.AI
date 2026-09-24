using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A decision client decorator that manages runtime context scope per-execution
/// and sets inline decision metadata in the runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="AskAsync"/> ensures a scope exists, populates it via contributors if newly
/// created, sets inline decision feature metadata (skipped only for a pass-through execution — see
/// <see cref="AIDecisionBuilder.AsPassThrough"/>), delegates to the inner client, and disposes any scope
/// it created.
/// </para>
/// <para>
/// Unlike <c>ScopedInlineSpeechToTextClient</c>/<c>ScopedInlineChatClient</c> — which only ever sit on
/// their capability's "create a client" path, where pass-through doesn't apply — <c>AIDecisionService</c>
/// (T8) also puts this wrapper on its execute path (<c>AIDecisionService.AskAsync&lt;TResponse&gt;(Action{AIDecisionBuilder}, AIDecisionQuestion{TResponse}, CancellationToken)</c>).
/// So the feature-metadata decision here follows the execute-path rule Chat's/SpeechToText's own execute
/// paths use (<c>!builder.IsPassThrough</c>), not the create-client-path rule those two
/// <c>Scoped*Inline*</c> wrappers use (<c>!scopeExisted</c>) — those two rules disagree in two cases: a
/// pass-through call made with no parent scope (old rule wrongly stamps metadata, new rule correctly
/// doesn't), and a normal, non-pass-through call made inside an already-existing parent scope (old rule
/// wrongly skips metadata, new rule correctly stamps it). This class must get both right.
/// </para>
/// <para>
/// This client does not publish notifications — that is the caller's (<c>IAIDecisionService</c>'s)
/// responsibility, same as <c>ScopedInlineSpeechToTextClient</c>.
/// </para>
/// </remarks>
internal sealed class ScopedInlineDecisionClient : IAIDecisionClient
{
    private readonly IAIDecisionClient _innerClient;
    private readonly AIDecisionBuilder _builder;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedInlineDecisionClient"/> class.
    /// </summary>
    /// <param name="innerClient">The base decision client to delegate to.</param>
    /// <param name="builder">The inline decision builder containing configuration.</param>
    /// <param name="contextAccessor">Accessor for the runtime context.</param>
    /// <param name="scopeProvider">Provider for creating runtime context scopes.</param>
    /// <param name="contributors">Collection of context contributors to populate the scope.</param>
    internal ScopedInlineDecisionClient(
        IAIDecisionClient innerClient,
        AIDecisionBuilder builder,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    }

    /// <inheritdoc />
    public async Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
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

            _builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !_builder.IsPassThrough);
            return await _innerClient.AskAsync(question, options, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == GetType())
        {
            return this;
        }

        return _innerClient.GetService(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public void Dispose() => _innerClient.Dispose();
}
