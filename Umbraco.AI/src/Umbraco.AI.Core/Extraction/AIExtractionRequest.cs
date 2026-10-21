using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// A batch of one or more typed <see cref="AIExtractionField"/>s to read out of the same shared text,
/// answered by an <see cref="IAIExtractionClient"/> in a single model call.
/// </summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIExtractionRequest
{
    /// <summary>
    /// The text to extract from, shared by every field in <see cref="Fields"/>. Must be non-blank —
    /// enforced by <see cref="ValidatingExtractionClient"/> before any provider is reached.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// The fields to extract, 1..n. Every field's <see cref="AIExtractionField.Id"/> must be a valid,
    /// unique identifier within the request — enforced by <see cref="ValidatingExtractionClient"/>
    /// before any provider is reached.
    /// </summary>
    public required IReadOnlyList<AIExtractionField> Fields { get; init; }

    /// <summary>
    /// The date that relative expressions in <see cref="State"/> ("yesterday", "next Friday") resolve
    /// against, when set.
    /// </summary>
    public DateOnly? ReferenceDate { get; init; }
}
