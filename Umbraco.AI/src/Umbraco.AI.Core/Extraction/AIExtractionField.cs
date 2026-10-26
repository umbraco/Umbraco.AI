using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// A typed field in an <see cref="AIExtractionRequest"/>. One concrete subclass exists per answer
/// shape — <see cref="AITextExtractionField"/>, <see cref="AINumberExtractionField"/>,
/// <see cref="AIDateExtractionField"/>, <see cref="AITimeExtractionField"/>,
/// <see cref="AIDateTimeExtractionField"/>, <see cref="AIDurationExtractionField"/> and
/// <see cref="AIListExtractionField"/> — so the field's own type is the discriminator, rather than a
/// separate <c>Type</c> enum paired with nullable per-kind properties.
/// </summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public abstract class AIExtractionField
{
    private string? _id;

    /// <summary>
    /// A correlation id, used to key the matching answer in <see cref="AIExtractionResponse.Answers"/>.
    /// Required, matching <c>^[A-Za-z_][A-Za-z0-9_]{0,63}$</c> and unique within the request, for every
    /// field sent via <c>IAIExtractionService.GetExtractionResponseAsync</c>; optional only for the
    /// single-field <c>IAIExtractionService.ExtractAsync</c> convenience, which assigns one internally
    /// when left null (see <see cref="WithId"/>).
    /// </summary>
    /// <remarks>
    /// Backed by a private field rather than an auto-property so <see cref="WithId"/> can assign it
    /// directly on a cloned instance — an <see langword="init"/> accessor alone only allows assignment
    /// from within an object initializer.
    /// </remarks>
    public string? Id { get => _id; init => _id = value; }

    /// <summary>What to extract — the natural-language instructions given to the model.</summary>
    public required string Instructions { get; init; }

    /// <summary>
    /// The <see cref="AIExtractionAnswer"/> subtype this field expects back. <see cref="IAIExtractionClient"/>
    /// stays non-generic, so this is how the non-generic pipeline checks a provider answered the shape
    /// this field actually asked for, without knowing <c>TAnswer</c> itself.
    /// </summary>
    /// <remarks>
    /// Internal, and implemented only by <see cref="AIExtractionField{TAnswer}"/> — new field kinds
    /// subclass that generic base, not this non-generic one directly.
    /// </remarks>
    internal abstract Type ExpectedAnswerType { get; }

    /// <summary>Whether <paramref name="answer"/> matches <see cref="ExpectedAnswerType"/>.</summary>
    internal bool IsExpectedAnswer(AIExtractionAnswer answer) => ExpectedAnswerType.IsInstanceOfType(answer);

    /// <summary>
    /// Returns a shallow copy of this field with <see cref="Id"/> set to <paramref name="id"/>, every
    /// other property preserved unchanged. A single clone on the base class, so any subclass is handled
    /// automatically: <see cref="object.MemberwiseClone"/> copies whatever the runtime type actually is.
    /// </summary>
    /// <param name="id">The id to assign on the returned clone.</param>
    internal AIExtractionField WithId(string id)
    {
        var clone = (AIExtractionField)MemberwiseClone();
        clone._id = id;
        return clone;
    }
}

/// <summary>
/// An <see cref="AIExtractionField"/> whose answer shape is known at compile time via
/// <typeparamref name="TAnswer"/>, letting <c>IAIExtractionService.ExtractAsync</c> return a strongly
/// typed answer with no cast.
/// </summary>
/// <typeparam name="TAnswer">The concrete <see cref="AIExtractionAnswer"/> this field yields.</typeparam>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public abstract class AIExtractionField<TAnswer> : AIExtractionField
    where TAnswer : AIExtractionAnswer
{
    /// <inheritdoc />
    internal override Type ExpectedAnswerType => typeof(TAnswer);
}
