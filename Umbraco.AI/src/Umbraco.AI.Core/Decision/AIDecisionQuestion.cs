using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A typed question to put to an <see cref="IAIDecisionClient"/>. One concrete subclass exists per
/// answer shape — <see cref="AIBinaryDecisionQuestion"/>, <see cref="AIChoiceDecisionQuestion"/>, and
/// <see cref="AIScoreDecisionQuestion"/> — so the question's own type is the discriminator, rather than
/// a separate <c>Kind</c> enum paired with nullable per-kind fields.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public abstract class AIDecisionQuestion
{
    /// <summary>What to decide — the natural-language instructions given to the model.</summary>
    public required string Instructions { get; init; }

    /// <summary>The content to judge against <see cref="Instructions"/>, when there is any.</summary>
    public string? Context { get; init; }
}

/// <summary>
/// An <see cref="AIDecisionQuestion"/> whose answer shape is known at compile time via
/// <typeparamref name="TResponse"/>, letting <c>IAIDecisionService.AskAsync</c> return a strongly typed
/// response with no cast.
/// </summary>
/// <typeparam name="TResponse">The concrete <see cref="AIDecisionResponse"/> this question yields.</typeparam>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public abstract class AIDecisionQuestion<TResponse> : AIDecisionQuestion
    where TResponse : AIDecisionResponse
{
}
