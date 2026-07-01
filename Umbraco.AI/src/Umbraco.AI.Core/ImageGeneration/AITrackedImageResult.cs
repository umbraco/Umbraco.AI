using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Wraps the result of a tracked image-generation operation together with the usage information
/// the service needs to record analytics and audit entries.
/// </summary>
/// <remarks>
/// Returned by the delegate passed to
/// <see cref="IAIImageGenerationService.InvokeWithTrackingAsync{TResult}"/>. The delegate typically
/// reaches the provider-native client (e.g. for masked outpainting) via <c>GetService</c>, performs the
/// raw call, and reports back the usage/image-count so the operation stays visible in analytics and audit.
/// </remarks>
/// <typeparam name="TResult">The caller-defined result of the operation.</typeparam>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public sealed class AITrackedImageResult<TResult>
{
    /// <summary>
    /// The caller-defined result of the operation (e.g. the generated image bytes, a media reference).
    /// </summary>
    public required TResult Result { get; init; }

    /// <summary>
    /// Optional token/usage details reported by the provider, recorded against analytics when present.
    /// </summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>
    /// Optional number of images produced by the operation, for telemetry enrichment.
    /// </summary>
    public int? ImageCount { get; init; }
}
