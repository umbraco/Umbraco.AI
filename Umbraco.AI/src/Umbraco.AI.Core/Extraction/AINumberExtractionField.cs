using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>An <see cref="AIExtractionField"/> that extracts a number.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AINumberExtractionField : AIExtractionField<AINumberExtractionAnswer>
{
    /// <summary>Rounds the value to this many decimal places, 0 to 6, when set.</summary>
    public int? Decimals { get; init; }

    /// <summary>Whether the value must be a whole number.</summary>
    public bool Integer { get; init; }

    /// <summary>The unit or currency the number is in, e.g. <c>"DKK"</c>, when it needs spelling out.</summary>
    public string? Unit { get; init; }
}
