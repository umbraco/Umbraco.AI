using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>The answer to an <see cref="AIChoiceDecisionQuestion"/>.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIChoiceDecisionAnswer : AIDecisionAnswer
{
    /// <summary>The selected option's <see cref="AIDecisionOption.Key"/>, exactly as supplied.</summary>
    public required string Choice { get; init; }

    /// <summary>The model's estimated probability for every option, keyed by <see cref="AIDecisionOption.Key"/>.</summary>
    public required IReadOnlyDictionary<string, double> Probabilities { get; init; }

    /// <summary>The provider's own confidence summary, from 0.0 to 1.0, when reported. Not comparable
    /// across providers.</summary>
    public double? Confidence { get; init; }
}
