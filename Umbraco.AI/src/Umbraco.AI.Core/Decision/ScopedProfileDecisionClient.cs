using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A decision client decorator that sets profile metadata in the runtime context per-execution.
/// </summary>
/// <remarks>
/// <para>
/// This client wraps a base <see cref="IAIDecisionClient"/> and automatically populates runtime context
/// with profile metadata (ProfileId, ProfileAlias, ProviderId, ModelId) whenever a decision is asked.
/// Mirrors <c>ScopedProfileSpeechToTextClient</c>.
/// </para>
/// </remarks>
internal sealed class ScopedProfileDecisionClient : IAIDecisionClient
{
    private readonly IAIDecisionClient _innerClient;
    private readonly AIProfile _profile;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public ScopedProfileDecisionClient(
        IAIDecisionClient innerClient,
        AIProfile profile,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    }

    /// <inheritdoc />
    public async Task<AIDecisionResponse> GetResponseAsync(
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            createdScope = AIRuntimeContextCallScope.Begin(_contextAccessor, _scopeProvider, _contributors, []);

            PopulateProfileMetadata();
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

    private void PopulateProfileMetadata()
    {
        var context = _contextAccessor.Context;
        if (context is null)
        {
            return;
        }

        context.SetProfileMetadata(_profile);
    }
}
