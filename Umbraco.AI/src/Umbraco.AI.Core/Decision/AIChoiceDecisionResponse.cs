using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>The answer to an <see cref="AIChoiceDecisionQuestion"/>.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIChoiceDecisionResponse : AIDecisionResponse
{
    /// <summary>The selected option's <see cref="AIDecisionOption.Key"/>.</summary>
    public required string Choice { get; init; }

    /// <summary>The model's confidence in <see cref="Choice"/>, from 0.0 to 1.0.</summary>
    public required double ChoiceConfidence { get; init; }

    /// <summary>The model's estimated probability for each option, keyed by <see cref="AIDecisionOption.Key"/>, when reported.</summary>
    public IReadOnlyDictionary<string, double> Probabilities { get; init; } = new Dictionary<string, double>();

    /// <inheritdoc />
    public override double Confidence => ChoiceConfidence;
}
