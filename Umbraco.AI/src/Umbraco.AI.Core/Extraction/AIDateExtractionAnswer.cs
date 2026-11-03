using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>The answer to an <see cref="AIDateExtractionField"/>.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIDateExtractionAnswer : AIExtractionAnswer
{
    /// <summary>The extracted value, or <see langword="null"/> when it isn't in the text.</summary>
    public DateOnly? Value { get; init; }

    /// <inheritdoc />
    public override bool IsFound => Value is not null;
}
