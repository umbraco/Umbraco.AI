using Umbraco.AI.Core.Providers.Errors;

#pragma warning disable UMBRACOAI_DECISION // Implements the experimental decision capability surface

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Builds the <see cref="AIProviderException"/> thrown when a provider answers a decision with the
/// wrong response shape.
/// </summary>
/// <remarks>
/// Two call sites need this exact exception — <see cref="AIErrorClassifyingDecisionClient"/> (the
/// primary check, made inside the tracking middleware so the failure is recorded) and
/// <see cref="AIDecisionService"/> (a narrowing guard afterwards, as defence in depth). Centralising the
/// construction here keeps the message/category from drifting between the two.
/// </remarks>
internal static class AIDecisionExceptionFactory
{
    internal static AIProviderException CreateResponseTypeMismatchException(Type expectedResponseType, AIDecisionResponse actualResponse) =>
        new(new AIProviderErrorInfo(
            AIProviderErrorCategory.Unknown,
            $"The AI provider returned a '{actualResponse.GetType().Name}' response, but this question expected a '{expectedResponseType.Name}'.",
            ProviderCode: null,
            RawMessage: $"Expected response type '{expectedResponseType.FullName}' but received '{actualResponse.GetType().FullName}'."));
}
