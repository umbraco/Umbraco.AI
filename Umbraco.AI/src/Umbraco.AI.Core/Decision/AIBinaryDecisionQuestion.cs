using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>A yes/no <see cref="AIDecisionQuestion"/>.</summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIBinaryDecisionQuestion : AIDecisionQuestion<AIBinaryDecisionResponse>
{
    /// <summary>What "yes" means, when it needs spelling out beyond <see cref="AIDecisionQuestion.Instructions"/>.</summary>
    public string? TrueCriteria { get; init; }

    /// <summary>What "no" means, when it needs spelling out beyond <see cref="AIDecisionQuestion.Instructions"/>.</summary>
    public string? FalseCriteria { get; init; }
}
