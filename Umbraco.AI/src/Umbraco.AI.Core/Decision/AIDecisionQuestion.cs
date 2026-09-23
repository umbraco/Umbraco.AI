using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A typed question to put to an <see cref="IAIDecisionClient"/>.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionQuestion
{
    /// <summary>The shape of answer this question expects.</summary>
    public required AIDecisionKind Kind { get; init; }

    /// <summary>The natural-language question to put to the model.</summary>
    public required string Prompt { get; init; }

    /// <summary>Required when <see cref="Kind"/> is <see cref="AIDecisionKind.Choice"/>.</summary>
    public IReadOnlyList<string>? Choices { get; init; }

    /// <summary>Optional bounds when <see cref="Kind"/> is <see cref="AIDecisionKind.Score"/>.</summary>
    public (double Min, double Max)? ScoreRange { get; init; }
}
