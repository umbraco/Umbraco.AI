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

    /// <summary>
    /// The <see cref="AIDecisionResponse"/> subtype this question expects back. <see cref="IAIDecisionClient"/>
    /// stays non-generic (see its remarks), so this is how the non-generic pipeline —
    /// <see cref="AIErrorClassifyingDecisionClient"/> — checks a provider answered the shape this
    /// question actually asked for, without knowing <c>TResponse</c> itself.
    /// </summary>
    /// <remarks>
    /// Internal, and implemented only by <see cref="AIDecisionQuestion{TResponse}"/> — third-party code
    /// adds a new question kind by subclassing that generic base (as this type's own remarks already
    /// document), not this non-generic one directly. That also means this non-generic base is not meant
    /// to be subclassed directly outside this assembly; an internal abstract member here enforces that
    /// intent rather than changing it.
    /// </remarks>
    internal abstract Type ExpectedResponseType { get; }

    /// <summary>Whether <paramref name="response"/> matches <see cref="ExpectedResponseType"/>.</summary>
    internal bool IsExpectedResponse(AIDecisionResponse response) => ExpectedResponseType.IsInstanceOfType(response);
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
    /// <inheritdoc />
    internal override Type ExpectedResponseType => typeof(TResponse);
}
