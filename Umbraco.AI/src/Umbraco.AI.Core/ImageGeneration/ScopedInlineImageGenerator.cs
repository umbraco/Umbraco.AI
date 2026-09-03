using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Internal plumbing for the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// An image-generator decorator that manages runtime context scope per-execution
/// and sets inline image-generation metadata in the runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="GenerateAsync"/> ensures a scope exists, populates it via contributors if newly
/// created, sets inline image-generation feature metadata (only when no parent scope already set it),
/// delegates to the inner generator, and disposes any scope it created. This mirrors the
/// <see cref="ScopedProfileImageGenerator"/> pattern.
/// </para>
/// <para>
/// This generator is returned by <see cref="IAIImageGenerationService.CreateImageGeneratorAsync"/>
/// and does not publish notifications.
/// </para>
/// </remarks>
internal sealed class ScopedInlineImageGenerator : AIBoundImageGeneratorBase
{
    private readonly AIImageGenerationBuilder _builder;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    internal ScopedInlineImageGenerator(
        IImageGenerator innerGenerator,
        AIImageGenerationBuilder builder,
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

    /// <inheritdoc />
    public override async Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(_builder.ContextItems ?? []);
                await _contributors.PopulateAsync(createdScope.Context, cancellationToken);
            }

            _builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !scopeExisted);
            return await base.GenerateAsync(request, options, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }
}
