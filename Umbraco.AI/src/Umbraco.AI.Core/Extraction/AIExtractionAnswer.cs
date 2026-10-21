using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// A typed answer to one field in an <see cref="AIExtractionRequest"/>. One concrete subclass exists
/// per answer shape, mirroring the corresponding <see cref="AIExtractionField"/> subclass.
/// </summary>
/// <remarks>
/// A value that isn't in the text comes back as a <see langword="null"/> value (an empty list for
/// <see cref="AIListExtractionAnswer"/>), never a guess.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public abstract class AIExtractionAnswer
{
    /// <summary>The model's confidence in the answer, from 0.0 to 1.0, when the provider reports one.</summary>
    public double? Confidence { get; init; }

    /// <summary>The model's estimated probability that the value is not in the text, from 0.0 to 1.0, when the provider reports one.</summary>
    public double? NullProbability { get; init; }

    /// <summary>
    /// Whether a value was found. Derived from the answer's value (non-null, or a non-empty list), so
    /// a provider never sets it.
    /// </summary>
    public abstract bool IsFound { get; }

    /// <summary>The provider's own, unmapped representation of this answer, when it chooses to expose one.</summary>
    public object? RawRepresentation { get; init; }
}
