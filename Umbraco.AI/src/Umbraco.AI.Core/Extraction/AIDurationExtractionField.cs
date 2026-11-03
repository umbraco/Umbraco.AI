using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>An <see cref="AIExtractionField"/> that extracts a length of time, as a number in a chosen unit.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIDurationExtractionField : AIExtractionField<AIDurationExtractionAnswer>
{
    /// <summary>The unit the duration is measured in, and returned in.</summary>
    public required AIDurationUnit Unit { get; init; }

    /// <summary>Rounds the value to this many decimal places, 0 to 6, when set.</summary>
    public int? Decimals { get; init; }

    /// <summary>Whether the value must be a whole number.</summary>
    public bool Integer { get; init; }
}
