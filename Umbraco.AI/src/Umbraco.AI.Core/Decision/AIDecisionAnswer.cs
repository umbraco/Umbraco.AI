using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A typed answer to one question in an <see cref="AIDecisionRequest"/>. One concrete subclass exists
/// per answer shape — <see cref="AIBinaryDecisionAnswer"/>, <see cref="AIChoiceDecisionAnswer"/>, and
/// <see cref="AIScoreDecisionAnswer"/> — mirroring the corresponding <see cref="AIDecisionQuestion"/>
/// subclass, rather than one flat shape with a <c>Kind</c> discriminator and nullable per-kind fields.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public abstract class AIDecisionAnswer
{
    /// <summary>The provider's own, unmapped representation of this answer, when it chooses to expose one.</summary>
    public object? RawRepresentation { get; init; }
}
