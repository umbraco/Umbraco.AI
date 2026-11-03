using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>The answer to an <see cref="AIListExtractionField"/>.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIListExtractionAnswer : AIExtractionAnswer
{
    /// <summary>The extracted items as invariant strings, or empty when none are in the text.</summary>
    public IReadOnlyList<string> Values { get; init; } = [];

    /// <summary>Where each item sits in the text, in the same order as <see cref="Values"/>, when the provider reports it.</summary>
    public IReadOnlyList<AIExtractionSpan> Spans { get; init; } = [];

    /// <inheritdoc />
    public override bool IsFound => Values.Count > 0;
}
