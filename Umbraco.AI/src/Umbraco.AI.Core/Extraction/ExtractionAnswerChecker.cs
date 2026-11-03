#pragma warning disable UMBRACOAI_EXTRACTION // Implements the experimental extraction capability surface

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Checks that a provider's <see cref="AIExtractionResponse"/> actually answers what an
/// <see cref="AIExtractionRequest"/> asked: one answer per field id, no extras, each of its field's
/// kind, and, when present, a confidence and null probability in range.
/// </summary>
/// <remarks>
/// A mismatch is a provider bug, reported as an <see cref="Providers.Errors.AIProviderException"/>.
/// Every field's <see cref="AIExtractionField.Id"/> is assumed already validated non-blank and unique
/// by <see cref="ValidatingExtractionClient"/>, outside this check.
/// </remarks>
internal static class ExtractionAnswerChecker
{
    /// <summary>
    /// Checks every field in <paramref name="request"/> was answered by an answer of its own kind, and
    /// that <paramref name="response"/> carries no answer for a field that wasn't asked.
    /// </summary>
    public static void Check(AIExtractionRequest request, AIExtractionResponse response)
    {
        CheckNoUnaskedAnswers(request, response);

        foreach (var field in request.Fields)
        {
            if (!response.Answers.TryGetValue(field.Id!, out var answer))
            {
                throw AIExtractionExceptionFactory.CreateMissingAnswerException(field);
            }

            if (!field.IsExpectedAnswer(answer))
            {
                throw AIExtractionExceptionFactory.CreateAnswerTypeMismatchException(field, answer);
            }

            CheckKindSpecifics(field, answer);
            CheckProbability(field, answer.Confidence, "confidence");
            CheckProbability(field, answer.NullProbability, "null probability");
        }
    }

    /// <summary>
    /// Checks the parts of an answer its type can't guarantee. A list's <c>Values</c> and <c>Spans</c> are
    /// non-null at compile time only, and a duration's <c>Unit</c> can differ from the one the field asked
    /// for: an adapter deserializing JSON can produce either.
    /// </summary>
    private static void CheckKindSpecifics(AIExtractionField field, AIExtractionAnswer answer)
    {
        switch (answer)
        {
            case AIListExtractionAnswer { Values: null }:
                throw AIExtractionExceptionFactory.CreateInvalidAnswerException(field, "a missing list of values");
            case AIListExtractionAnswer { Spans: null }:
                throw AIExtractionExceptionFactory.CreateInvalidAnswerException(field, "a missing list of spans");
            case AIDurationExtractionAnswer duration
                when field is AIDurationExtractionField durationField && duration.Unit != durationField.Unit:
                throw AIExtractionExceptionFactory.CreateInvalidAnswerException(
                    field, $"a duration in {duration.Unit} when {durationField.Unit} was asked for");
        }
    }

    private static void CheckNoUnaskedAnswers(AIExtractionRequest request, AIExtractionResponse response)
    {
        var askedIds = request.Fields.Select(f => f.Id!).ToHashSet(StringComparer.Ordinal);

        foreach (var answerId in response.Answers.Keys)
        {
            if (!askedIds.Contains(answerId))
            {
                throw AIExtractionExceptionFactory.CreateExtraAnswerException(answerId);
            }
        }
    }

    /// <summary>
    /// Checks an optional <paramref name="value"/> is in [0, 1]. Written as a positive range test —
    /// not <c>value &lt; 0 || value &gt; 1</c> — because every comparison against <see cref="double.NaN"/>
    /// is <see langword="false"/>, which would let a NaN slip past a negated check.
    /// </summary>
    private static void CheckProbability(AIExtractionField field, double? value, string label)
    {
        if (value is { } v && !(v >= 0 && v <= 1))
        {
            throw AIExtractionExceptionFactory.CreateInvalidAnswerException(field, $"a {label} of {v} outside [0, 1]");
        }
    }
}
