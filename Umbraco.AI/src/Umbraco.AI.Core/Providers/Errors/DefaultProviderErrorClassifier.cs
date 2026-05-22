using System.Net.Http;

namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// Classifies BCL exception types that don't require an SDK dependency.
/// Acts as the floor under per-provider classifiers in
/// <see cref="AIProviderErrorClassifier"/>.
/// </summary>
/// <remarks>
/// Handles cancellation, timeouts, and <see cref="HttpRequestException"/> — which the OpenAI SDK
/// and others wrap for transport-level failures and which exposes a typed <c>StatusCode</c>
/// property since .NET 5.
/// </remarks>
public sealed class DefaultProviderErrorClassifier : IAIProviderErrorClassifier
{
    /// <inheritdoc />
    public AIProviderErrorInfo? Classify(Exception exception)
    {
        foreach (var ex in EnumerateChain(exception))
        {
            switch (ex)
            {
                case OperationCanceledException:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.Cancelled,
                        "The request was cancelled.",
                        ProviderCode: null,
                        exception.Message);

                case TimeoutException:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.Transient,
                        "The AI service took too long to respond. Try again in a moment.",
                        ProviderCode: "timeout",
                        exception.Message);

                case HttpRequestException { StatusCode: { } code }:
                    return ProviderErrorMapping.FromHttpStatus((int)code, exception.Message);

                case HttpRequestException:
                    return new AIProviderErrorInfo(
                        AIProviderErrorCategory.NetworkError,
                        "Couldn't reach the AI service. Check your connection and try again.",
                        ProviderCode: null,
                        exception.Message);
            }
        }

        return null;
    }

    private static IEnumerable<Exception> EnumerateChain(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }
}
