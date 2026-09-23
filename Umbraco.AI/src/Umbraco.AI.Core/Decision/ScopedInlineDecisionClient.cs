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
/// created, sets inline decision feature metadata (only when no parent scope already set it), delegates
/// to the inner client, and disposes any scope it created. Mirrors
/// <c>ScopedInlineSpeechToTextClient</c>/<c>ScopedProfileDecisionClient</c>.
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

            _builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !scopeExisted);
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
