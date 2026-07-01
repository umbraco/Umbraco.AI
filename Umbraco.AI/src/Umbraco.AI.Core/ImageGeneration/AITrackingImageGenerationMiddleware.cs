using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Wraps the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Image-generation middleware that records usage analytics and audit entries for generation
/// operations, via the shared <see cref="AIImageGenerationTracker"/>.
/// </summary>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
internal sealed class AITrackingImageGenerationMiddleware : IAIImageGenerationMiddleware
{
    private readonly AIImageGenerationTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="AITrackingImageGenerationMiddleware"/> class.
    /// </summary>
    public AITrackingImageGenerationMiddleware(AIImageGenerationTracker tracker)
    {
        _tracker = tracker;
    }

    /// <inheritdoc />
    public IImageGenerator Apply(IImageGenerator generator)
        => new AITrackingImageGenerationClient(generator, _tracker);
}
