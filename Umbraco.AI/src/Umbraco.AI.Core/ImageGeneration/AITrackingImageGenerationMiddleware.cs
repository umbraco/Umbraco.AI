using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Observability;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Wraps the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Image-generation middleware that records usage analytics and audit entries for generation
/// operations, via the shared <see cref="IAIOperationTracker"/>.
/// </summary>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
internal sealed class AITrackingImageGenerationMiddleware : IAIImageGenerationMiddleware
{
    private readonly IAIOperationTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="AITrackingImageGenerationMiddleware"/> class.
    /// </summary>
    public AITrackingImageGenerationMiddleware(IAIOperationTracker tracker) => _tracker = tracker;

    /// <inheritdoc />
    public IImageGenerator Apply(IImageGenerator generator)
        => new AITrackingImageGenerationClient(generator, _tracker);
}
