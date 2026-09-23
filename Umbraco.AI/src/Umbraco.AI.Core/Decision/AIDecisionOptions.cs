using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Per-call options for <see cref="IAIDecisionClient.AskAsync"/>.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionOptions
{
    /// <summary>Overrides the model configured on the profile/connection for this call, when set.</summary>
    public string? ModelId { get; init; }
}
