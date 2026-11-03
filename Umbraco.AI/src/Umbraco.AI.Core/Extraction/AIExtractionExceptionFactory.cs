using Umbraco.AI.Core.Providers.Errors;

#pragma warning disable UMBRACOAI_EXTRACTION // Implements the experimental extraction capability surface

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Builds the <see cref="AIProviderException"/>s thrown when a provider's answer doesn't match what a
/// field asked for.
/// </summary>
/// <remarks>
/// Centralised so the message and category can't drift between <see cref="ExtractionAnswerChecker"/>
/// and any later narrowing guard.
/// </remarks>
internal static class AIExtractionExceptionFactory
{
    internal static AIProviderException CreateAnswerTypeMismatchException(AIExtractionField field, AIExtractionAnswer actualAnswer) =>
        new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider answered field '{Truncate(field.Id)}' with a '{actualAnswer.GetType().Name}', but it expected a '{field.ExpectedAnswerType.Name}'.",
            ProviderCode: null,
            RawMessage: $"Field '{Truncate(field.Id)}' expected answer type '{field.ExpectedAnswerType.FullName}' but received '{actualAnswer.GetType().FullName}'."));

    internal static AIProviderException CreateMissingAnswerException(AIExtractionField field) =>
        new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider did not answer field '{Truncate(field.Id)}'.",
            ProviderCode: null,
            RawMessage: $"No answer was returned for field id '{Truncate(field.Id)}'."));

    internal static AIProviderException CreateExtraAnswerException(string answerId)
    {
        var truncatedId = Truncate(answerId);
        return new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider answered field '{truncatedId}', which wasn't asked.",
            ProviderCode: null,
            RawMessage: $"Received an answer keyed '{truncatedId}', which is not among the request's field ids."));
    }

    /// <summary>
    /// Builds the exception for an inconsistency <see cref="ExtractionAnswerChecker"/> finds within an
    /// otherwise correctly-shaped answer (an out-of-range confidence or null probability).
    /// </summary>
    /// <param name="field">The field whose answer is invalid.</param>
    /// <param name="reason">A plain-English description of what's wrong with the answer.</param>
    internal static AIProviderException CreateInvalidAnswerException(AIExtractionField field, string reason) =>
        new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider answered field '{Truncate(field.Id)}' with {reason}.",
            ProviderCode: null,
            RawMessage: $"Field '{Truncate(field.Id)}': {reason}."));

    /// <summary>
    /// Truncates a provider-supplied string (an answer id) to <paramref name="maxLength"/> characters,
    /// with an ellipsis, before it goes into an exception message. Null-safe (renders as
    /// <c>"(null)"</c>).
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
