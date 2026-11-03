using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable UMBRACOAI_EXTRACTION // IAIExtractionClient is experimental

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// An extraction client decorator that manages runtime context scope per-execution
/// and sets inline extraction metadata in the runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="GetResponseAsync"/> opens the runtime context via
/// <see cref="AIRuntimeContextCallScope.Begin"/>, sets inline extraction feature metadata (skipped only for
/// a pass-through execution — see <see cref="AIExtractionBuilder.AsPassThrough"/>), delegates to the inner
/// client, and disposes any scope it created.
/// </para>
/// <para>
/// Unlike <c>ScopedInlineSpeechToTextClient</c>/<c>ScopedInlineChatClient</c> — which only ever sit on
/// their capability's "create a client" path, where pass-through doesn't apply — <c>AIExtractionService</c>
/// also puts this wrapper on its execute path (<c>AIExtractionService.GetExtractionResponseAsync(Action{AIExtractionBuilder}, AIExtractionRequest, CancellationToken)</c>,
/// which both the typed <c>ExtractAsync</c> overloads and direct batch callers funnel through).
/// So the feature-metadata decision here always follows the execute-path rule Chat's/SpeechToText's own
/// execute paths use (<c>!builder.IsPassThrough</c>), not the create-client-path rule those two
/// <c>Scoped*Inline*</c> wrappers use (<c>!hadContext</c>) — those two rules disagree in two cases: a
/// pass-through call made with no parent scope (the create-client rule would wrongly stamp metadata, the
/// execute rule correctly doesn't), and a normal, non-pass-through call made inside an already-existing
/// parent scope (the create-client rule would wrongly skip metadata, the execute rule correctly stamps
/// it). This class must get both right.
/// </para>
/// <para>
/// This client does not publish notifications — that is the caller's (<c>IAIExtractionService</c>'s)
/// responsibility, same as <c>ScopedInlineSpeechToTextClient</c>.
/// </para>
/// </remarks>
internal sealed class ScopedInlineExtractionClient : IAIExtractionClient
{
    private readonly IAIExtractionClient _innerClient;
    private readonly AIExtractionBuilder _builder;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedInlineExtractionClient"/> class.
    /// </summary>
    /// <param name="innerClient">The base extraction client to delegate to.</param>
    /// <param name="builder">The inline extraction builder containing configuration.</param>
    /// <param name="contextAccessor">Accessor for the runtime context.</param>
    /// <param name="scopeProvider">Provider for creating runtime context scopes.</param>
    /// <param name="contributors">Collection of context contributors to populate the scope.</param>
    internal ScopedInlineExtractionClient(
        IAIExtractionClient innerClient,
        AIExtractionBuilder builder,
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
    public async Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            createdScope = AIRuntimeContextCallScope.Begin(_contextAccessor, _scopeProvider, _contributors, _builder.ContextItems ?? []);

            _builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !_builder.IsPassThrough);
            return await _innerClient.GetResponseAsync(request, options, cancellationToken);
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
