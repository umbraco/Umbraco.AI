using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// An embedding generator decorator that manages runtime context scope per-execution
/// and sets inline embedding metadata in the runtime context.
/// </summary>
internal sealed class ScopedInlineEmbeddingGenerator : AIBoundEmbeddingGeneratorBase<string, Embedding<float>>
{
    private readonly AIEmbeddingBuilder _builder;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    internal ScopedInlineEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        AIEmbeddingBuilder builder,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
        : base(innerGenerator)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    }

    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
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
            return await base.GenerateAsync(values, options, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }
}
