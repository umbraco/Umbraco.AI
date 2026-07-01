using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Base class for image-generator decorators that wrap cross-cutting concerns.
/// </summary>
/// <remarks>
/// <para>
/// Derives from <see cref="DelegatingImageGenerator"/>, which forwards <c>GenerateAsync</c>,
/// <c>GetService</c>, and <c>Dispose</c> to the inner generator by default. The <c>GetService</c>
/// forwarding is load-bearing: it is what lets a consumer resolve the provider-native client
/// (e.g. <c>OpenAI.Images.ImageClient</c> / <c>OpenAI.OpenAIClient</c>) through the full scoped +
/// middleware pipeline for masked outpainting. Subclasses must not break this forwarding.
/// </para>
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public abstract class AIBoundImageGeneratorBase : DelegatingImageGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIBoundImageGeneratorBase"/> class.
    /// </summary>
    /// <param name="innerGenerator">The inner image generator to delegate to.</param>
    protected AIBoundImageGeneratorBase(IImageGenerator innerGenerator)
        : base(innerGenerator)
    {
    }
}
