using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Per-call options for <see cref="IAIExtractionClient.GetResponseAsync"/>.
/// </summary>
/// <remarks>
/// Mirrors M.E.AI's own per-request options types (<c>ChatOptions</c>, <c>SpeechToTextOptions</c>): a
/// mutable class with a <see cref="Clone"/> method, rather than an immutable record.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIExtractionOptions
{
    /// <summary>Overrides the model configured on the profile/connection for this call, when set.</summary>
    public string? ModelId { get; set; }

    /// <summary>Creates a shallow copy of the current <see cref="AIExtractionOptions"/> instance.</summary>
    public AIExtractionOptions Clone() => new() { ModelId = ModelId };
}
