using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// An embedding generator decorator that sets profile metadata in the runtime context per-execution.
/// </summary>
/// <remarks>
/// <para>
/// This generator wraps a base <see cref="IEmbeddingGenerator{String, Embedding}"/> and automatically
/// populates runtime context with profile metadata (ProfileId, ProfileAlias, ProviderId, ModelId)
/// whenever an embedding operation is executed.
/// </para>
/// <para>
/// This decorator is used by <see cref="IAIEmbeddingGeneratorFactory"/> to ensure profile metadata is
/// available in the runtime context for middleware, logging, and telemetry purposes.
/// </para>
/// <para>
/// <strong>Scope Management:</strong>
/// </para>
/// <list type="bullet">
///   <item>If an active scope exists, uses it to set metadata (e.g., when called from a service)</item>
///   <item>If no scope exists, creates a temporary scope for the execution</item>
///   <item>Automatically disposes any scope it creates after execution completes</item>
/// </list>
/// </remarks>
internal sealed class ScopedProfileEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly AIProfile _profile;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public ScopedProfileEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        AIProfile profile,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
        : base(innerGenerator)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            createdScope = AIRuntimeContextCallScope.Begin(_contextAccessor, _scopeProvider, _contributors, []);

            PopulateProfileMetadata();
            return await base.GenerateAsync(values, options, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

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
