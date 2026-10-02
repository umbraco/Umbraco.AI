using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>The answer to an <see cref="AIBinaryDecisionQuestion"/>.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIBinaryDecisionAnswer : AIDecisionAnswer
{
    /// <summary>The model's estimated probability that the answer is "yes", from 0.0 to 1.0. No
    /// separate <c>Confidence</c> — the probability itself is the distribution.</summary>
    public required double TrueProbability { get; init; }

    /// <summary>Whether <see cref="TrueProbability"/> is at least <paramref name="threshold"/>. The
    /// cut-off is the caller's choice, not a fixed 0.5 baked into the answer.</summary>
    /// <param name="threshold">The minimum probability counted as "yes". Defaults to 0.5.</param>
    public bool IsTrue(double threshold = 0.5) => TrueProbability >= threshold;
}
