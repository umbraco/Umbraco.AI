using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A typed question in an <see cref="AIDecisionRequest"/>. One concrete subclass exists per answer
/// shape — <see cref="AIBinaryDecisionQuestion"/>, <see cref="AIChoiceDecisionQuestion"/>, and
/// <see cref="AIScoreDecisionQuestion"/> — so the question's own type is the discriminator, rather than
/// a separate <c>Kind</c> enum paired with nullable per-kind fields.
/// </summary>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public abstract class AIDecisionQuestion
{
    private string? _id;

    /// <summary>
    /// A correlation id, used to key the matching answer in <see cref="AIDecisionResponse.Answers"/>.
    /// Required and must be unique within the request for every question sent via
    /// <c>IAIDecisionService.GetDecisionResponseAsync</c> — including a single-question request; optional
    /// only for the single-question <c>IAIDecisionService.AskAsync</c> convenience, which assigns one
    /// internally when left null (see <see cref="WithId"/>).
    /// </summary>
    /// <remarks>
    /// Backed by a private field rather than an auto-property so <see cref="WithId"/> can assign it
    /// directly on a cloned instance — an <see langword="init"/> accessor alone only allows assignment
    /// from within an object initializer.
    /// </remarks>
    public string? Id { get => _id; init => _id = value; }

    /// <summary>What to decide — the natural-language instructions given to the model.</summary>
    public required string Instructions { get; init; }

    /// <summary>
    /// The <see cref="AIDecisionAnswer"/> subtype this question expects back. <see cref="IAIDecisionClient"/>
    /// stays non-generic (see its remarks), so this is how the non-generic pipeline —
    /// <see cref="AIErrorClassifyingDecisionClient"/> — checks a provider answered the shape this
    /// question actually asked for, without knowing <c>TAnswer</c> itself.
    /// </summary>
    /// <remarks>
    /// Internal, and implemented only by <see cref="AIDecisionQuestion{TAnswer}"/> — third-party code
    /// adds a new question kind by subclassing that generic base (as this type's own remarks already
    /// document), not this non-generic one directly. That also means this non-generic base is not meant
    /// to be subclassed directly outside this assembly; an internal abstract member here enforces that
    /// intent rather than changing it.
    /// </remarks>
    internal abstract Type ExpectedAnswerType { get; }

    /// <summary>Whether <paramref name="answer"/> matches <see cref="ExpectedAnswerType"/>.</summary>
    internal bool IsExpectedAnswer(AIDecisionAnswer answer) => ExpectedAnswerType.IsInstanceOfType(answer);

    /// <summary>
    /// Returns a shallow copy of this question with <see cref="Id"/> set to <paramref name="id"/>, every
    /// other property preserved unchanged. A single clone on the base class — rather than a per-kind
    /// switch — so a new built-in question kind, or a third-party subclass this assembly knows nothing
    /// about, is handled automatically: <see cref="object.MemberwiseClone"/> copies whatever the runtime
    /// type actually is.
    /// </summary>
    /// <param name="id">The id to assign on the returned clone.</param>
    internal AIDecisionQuestion WithId(string id)
    {
        var clone = (AIDecisionQuestion)MemberwiseClone();
        clone._id = id;
        return clone;
    }
}

/// <summary>
/// An <see cref="AIDecisionQuestion"/> whose answer shape is known at compile time via
/// <typeparamref name="TAnswer"/>, letting <c>IAIDecisionService.AskAsync</c> return a strongly typed
/// answer with no cast.
/// </summary>
/// <typeparam name="TAnswer">The concrete <see cref="AIDecisionAnswer"/> this question yields.</typeparam>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public abstract class AIDecisionQuestion<TAnswer> : AIDecisionQuestion
    where TAnswer : AIDecisionAnswer
{
    /// <inheritdoc />
    internal override Type ExpectedAnswerType => typeof(TAnswer);
}
