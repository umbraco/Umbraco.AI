namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// Classifies exceptions thrown by AI provider SDKs into a normalised <see cref="AIProviderErrorInfo"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are typically scoped to a single provider SDK (e.g. Anthropic, AWS Bedrock)
/// and return <c>null</c> for exceptions they don't recognise so the composite classifier can
/// fall through to the next candidate.
/// </para>
/// <para>
/// The composite registered as <see cref="IAIProviderErrorClassifier"/> guarantees a non-null
/// result by falling back to a generic <see cref="AIProviderErrorCategory.Unknown"/> entry.
/// </para>
/// </remarks>
public interface IAIProviderErrorClassifier
{
    /// <summary>
    /// Attempts to classify the given exception.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>
    /// A populated <see cref="AIProviderErrorInfo"/> if the classifier recognises the exception;
    /// otherwise <c>null</c> to indicate the next classifier should be tried.
    /// </returns>
    AIProviderErrorInfo? Classify(Exception exception);
}
