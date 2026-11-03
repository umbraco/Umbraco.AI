using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>The answer to an <see cref="AIDurationExtractionField"/>.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIDurationExtractionAnswer : AIExtractionAnswer
{
    /// <summary>The extracted value, or <see langword="null"/> when it isn't in the text.</summary>
    public decimal? Value { get; init; }

    /// <summary>The unit <see cref="Value"/> is in: the unit the field asked for.</summary>
    public required AIDurationUnit Unit { get; init; }

    /// <inheritdoc />
    public override bool IsFound => Value is not null;
}
