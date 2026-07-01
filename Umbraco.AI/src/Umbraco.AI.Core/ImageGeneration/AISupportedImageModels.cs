using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// The image-generation models available for a resolved profile, plus the model the profile is bound to.
/// </summary>
/// <remarks>
/// Consumers use this to validate sizes/parameters up front: each <see cref="AIModelDescriptor"/> carries
/// per-model image constraints (e.g. supported sizes, multiple-of-16, max edge) in its
/// <see cref="AIModelDescriptor.Metadata"/>, and <see cref="ModelId"/> identifies the model the profile selected.
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public sealed class AISupportedImageModels
{
    /// <summary>
    /// The image-generation models exposed by the profile's provider/connection, with constraint metadata.
    /// </summary>
    public required IReadOnlyList<AIModelDescriptor> Models { get; init; }

    /// <summary>
    /// The model ID the resolved profile is bound to.
    /// </summary>
    public required string ModelId { get; init; }
}
