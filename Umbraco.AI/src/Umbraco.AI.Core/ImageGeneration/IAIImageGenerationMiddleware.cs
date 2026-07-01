using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Defines middleware that can be applied to AI image generators.
/// Middleware can implement cross-cutting concerns like logging, telemetry, usage recording, etc.
/// </summary>
/// <remarks>
/// The order of middleware execution is controlled by the <see cref="AIImageGenerationMiddlewareCollectionBuilder"/>
/// using <c>Append</c>, <c>InsertBefore</c>, and <c>InsertAfter</c> methods.
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public interface IAIImageGenerationMiddleware
{
    /// <summary>
    /// Applies this middleware to the given image generator.
    /// </summary>
    /// <param name="generator">The image generator to wrap with middleware.</param>
    /// <returns>The wrapped image generator with middleware applied.</returns>
    IImageGenerator Apply(IImageGenerator generator);
}
