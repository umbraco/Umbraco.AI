using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>The answer to an <see cref="AITextExtractionField"/>.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AITextExtractionAnswer : AIExtractionAnswer
{
    /// <summary>The extracted value, or <see langword="null"/> when it isn't in the text.</summary>
    public string? Value { get; init; }

    /// <summary>Where <see cref="Value"/> sits in the text, when the provider reports it.</summary>
    public AIExtractionSpan? Span { get; init; }

    /// <inheritdoc />
    public override bool IsFound => Value is not null;
}
