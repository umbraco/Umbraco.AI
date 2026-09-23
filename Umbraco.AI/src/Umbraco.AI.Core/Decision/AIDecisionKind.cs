using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// The shape of answer a <see cref="AIDecisionQuestion"/> expects and a <see cref="AIDecisionResponse"/>
/// carries.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public enum AIDecisionKind
{
    /// <summary>A yes/no answer.</summary>
    Binary,

    /// <summary>An answer selected from a fixed set of choices.</summary>
    Choice,

    /// <summary>A numeric score, optionally bounded by a range.</summary>
    Score,
}
