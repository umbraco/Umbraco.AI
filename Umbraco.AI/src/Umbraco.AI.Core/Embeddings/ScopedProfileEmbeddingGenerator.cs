using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;

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
/// <strong>Scope Management:</strong>
/// </para>
/// <list type="bullet">
///   <item>If an active scope exists, uses it to set metadata</item>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedProfileEmbeddingGenerator"/> class.
    /// </summary>
    /// <param name="innerGenerator">The base embedding generator to wrap.</param>
    /// <param name="profile">The profile containing metadata to set in the runtime context.</param>
    /// <param name="contextAccessor">Accessor for the runtime context.</param>
    /// <param name="scopeProvider">Provider for creating runtime context scopes.</param>
    /// <param name="contributors">Collection of context contributors to populate the scope.</param>
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
        var scopeExisted = _contextAccessor.Context != null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope([]);
                _contributors.Populate(createdScope.Context);
            }

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

        context.SetValue(Constants.ContextKeys.ProfileId, _profile.Id);
        context.SetValue(Constants.ContextKeys.ProfileAlias, _profile.Alias);
        context.SetValue(Constants.ContextKeys.ProfileVersion, _profile.Version);
        context.SetValue(Constants.ContextKeys.ProviderId, _profile.Model.ProviderId);
        context.SetValue(Constants.ContextKeys.ModelId, _profile.Model.ModelId);
    }
}
