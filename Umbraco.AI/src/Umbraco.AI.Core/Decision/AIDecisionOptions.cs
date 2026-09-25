using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Per-call options for <see cref="IAIDecisionClient.AskAsync"/>.
/// </summary>
/// <remarks>
/// Mirrors M.E.AI's own per-request options types (<c>ChatOptions</c>, <c>SpeechToTextOptions</c>): a
/// mutable class with a <see cref="Clone"/> method, rather than an immutable record. <see cref="IAIDecisionClient"/>
/// stands in for what M.E.AI would provide if it had this abstraction, so it follows M.E.AI's own
/// conventions instead of diverging into an idiomatic-but-different C# shape.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionOptions
{
    /// <summary>Overrides the model configured on the profile/connection for this call, when set.</summary>
    public string? ModelId { get; set; }

    /// <summary>Creates a shallow copy of the current <see cref="AIDecisionOptions"/> instance.</summary>
    public AIDecisionOptions Clone() => new() { ModelId = ModelId };
}
