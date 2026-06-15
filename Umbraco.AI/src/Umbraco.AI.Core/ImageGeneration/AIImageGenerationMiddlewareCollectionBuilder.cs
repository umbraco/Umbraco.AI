using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Composing;

#pragma warning disable UMBRACOAI_IMAGEGEN // Builder for the experimental image-generation middleware collection

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// An ordered collection builder for AI image-generation middleware.
/// </summary>
/// <remarks>
/// Use this builder to configure the order of middleware in the image-generation pipeline:
/// <code>
/// builder.AIImageGenerationMiddleware()
///     .Append&lt;LoggingImageGenerationMiddleware&gt;()
///     .Append&lt;CachingMiddleware&gt;();
/// </code>
/// Middleware is applied in collection order when wrapping the underlying image generator.
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public class AIImageGenerationMiddlewareCollectionBuilder
    : OrderedCollectionBuilderBase<AIImageGenerationMiddlewareCollectionBuilder, AIImageGenerationMiddlewareCollection, IAIImageGenerationMiddleware>
{
    /// <inheritdoc />
    protected override AIImageGenerationMiddlewareCollectionBuilder This => this;
}
