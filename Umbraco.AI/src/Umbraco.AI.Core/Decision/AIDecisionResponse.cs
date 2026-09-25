using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A typed answer returned by an <see cref="IAIDecisionClient"/>. One concrete subclass exists per
/// answer shape — <see cref="AIBinaryDecisionResponse"/>, <see cref="AIChoiceDecisionResponse"/>, and
/// <see cref="AIScoreDecisionResponse"/> — mirroring the corresponding <see cref="AIDecisionQuestion"/>
/// subclass, rather than one flat shape with a <c>Kind</c> discriminator and nullable per-kind fields.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public abstract class AIDecisionResponse
{
    /// <summary>The model that produced this answer, when known.</summary>
    public string? ModelId { get; init; }

    /// <summary>Token/usage counts for this call, when reported by the provider.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>The provider's own, unmapped representation of this response, when it chooses to expose one.</summary>
    public object? RawRepresentation { get; init; }

    /// <summary>The model's confidence in this answer, from 0.0 to 1.0.</summary>
    public abstract double Confidence { get; }
}
