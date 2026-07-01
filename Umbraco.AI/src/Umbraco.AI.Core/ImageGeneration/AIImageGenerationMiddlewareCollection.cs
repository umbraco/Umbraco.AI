using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Composing;

#pragma warning disable UMBRACOAI_IMAGEGEN // Collection of the experimental image-generation middleware

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// A collection of image-generation middleware applied in order to AI image generators.
/// </summary>
/// <remarks>
/// The order of middleware in this collection is controlled by the
/// <see cref="AIImageGenerationMiddlewareCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods.
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public sealed class AIImageGenerationMiddlewareCollection : BuilderCollectionBase<IAIImageGenerationMiddleware>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIImageGenerationMiddlewareCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the middleware instances.</param>
    public AIImageGenerationMiddlewareCollection(Func<IEnumerable<IAIImageGenerationMiddleware>> items)
        : base(items)
    { }
}
