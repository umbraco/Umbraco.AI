using Umbraco.AI.Core.Providers.Errors;

#pragma warning disable UMBRACOAI_DECISION // Implements the experimental decision capability surface

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Builds the <see cref="AIProviderException"/>s thrown when a provider's answer doesn't match what a
/// question asked for.
/// </summary>
/// <remarks>
/// Used by <see cref="AIErrorClassifyingDecisionClient"/> — made inside the tracking middleware so the
/// failure is recorded (see its remarks and the <c>provider-contract-checks-inside-tracking</c> memory
/// entry) — and by <see cref="AIDecisionService"/>'s narrowing guard afterwards, as defence in depth.
/// Centralising the construction here keeps the message/category from drifting between the two.
/// </remarks>
internal static class AIDecisionExceptionFactory
{
    internal static AIProviderException CreateAnswerTypeMismatchException(AIDecisionQuestion question, AIDecisionAnswer actualAnswer) =>
        new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider answered question '{question.Id}' with a '{actualAnswer.GetType().Name}', but it expected a '{question.ExpectedAnswerType.Name}'.",
            ProviderCode: null,
            RawMessage: $"Question '{question.Id}' expected answer type '{question.ExpectedAnswerType.FullName}' but received '{actualAnswer.GetType().FullName}'."));

    internal static AIProviderException CreateAnswerTypeMismatchException(Type expectedAnswerType, AIDecisionAnswer actualAnswer) =>
        new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider returned a '{actualAnswer.GetType().Name}' answer, but this question expected a '{expectedAnswerType.Name}'.",
            ProviderCode: null,
            RawMessage: $"Expected answer type '{expectedAnswerType.FullName}' but received '{actualAnswer.GetType().FullName}'."));

    internal static AIProviderException CreateMissingAnswerException(AIDecisionQuestion question) =>
        new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider did not answer question '{question.Id}'.",
            ProviderCode: null,
            RawMessage: $"No answer was returned for question id '{question.Id}'."));

    internal static AIProviderException CreateExtraAnswerException(string answerId)
    {
        var truncatedId = Truncate(answerId);
        return new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider answered question '{truncatedId}', which wasn't asked.",
            ProviderCode: null,
            RawMessage: $"Received an answer keyed '{truncatedId}', which is not among the request's question ids."));
    }

    /// <summary>
    /// Builds the exception for every inconsistency <see cref="DecisionAnswerChecker"/> finds within an
    /// otherwise correctly-shaped answer (an out-of-range probability, an incomplete distribution, an
    /// unrecognized choice, a distribution that doesn't sum to 1, an out-of-range score or confidence).
    /// One shared method, rather than one per rule, since every one of those is the same shape of
    /// problem — an answer that doesn't hold together — and <paramref name="reason"/> already carries
    /// what makes this one specific.
    /// </summary>
    /// <param name="question">The question whose answer is invalid.</param>
    /// <param name="reason">A plain-English description of what's wrong with the answer.</param>
    internal static AIProviderException CreateInvalidAnswerException(AIDecisionQuestion question, string reason) =>
        new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider answered question '{question.Id}' with {reason}.",
            ProviderCode: null,
            RawMessage: $"Question '{question.Id}': {reason}."));

    /// <summary>
    /// Truncates a provider-supplied string (an answer id or a free-text choice) to
    /// <paramref name="maxLength"/> characters, with an ellipsis, before it goes into an exception
    /// message — a misbehaving provider's "answer" could otherwise be arbitrarily large. Null-safe
    /// (renders as <c>"(null)"</c>) since a provider-supplied value isn't guaranteed non-null even where
    /// its declared type says <see langword="required"/> — <c>required</c> is a compile-time guarantee,
    /// not one JSON deserialization enforces.
    /// </summary>
    internal static string Truncate(string? value, int maxLength = 100)
    {
        if (value is null)
        {
            return "(null)";
        }

        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
