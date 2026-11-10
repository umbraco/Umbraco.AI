using Umbraco.AI.Core.Providers.Errors;
using Umbraco.Automate.Core.Actions;

namespace Umbraco.AI.Automate.Helpers;

/// <summary>
/// Maps an AI provider failure onto the <see cref="StepRunErrorCategory"/> Automate records for the
/// step and uses to decide whether a retry can help.
/// </summary>
/// <remarks>
/// Automate skips the retry budget for terminal categories (validation, configuration,
/// authentication, cancellation) and retries everything else. Reporting every AI failure as
/// <see cref="StepRunErrorCategory.Unknown"/> therefore retried requests that can never succeed, such
/// as a rejected API key or an unknown model, and spent provider quota doing so.
/// </remarks>
internal static class StepErrorCategoryMapping
{
    /// <summary>
    /// Finds the classified <see cref="AIProviderException"/> in the exception chain and maps its
    /// category. Returns <see cref="StepRunErrorCategory.Unknown"/> when the failure did not come
    /// from an AI provider.
    /// </summary>
    public static StepRunErrorCategory FromException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is AIProviderException providerError)
            {
                return IsOutOfCredit(providerError)
                    ? StepRunErrorCategory.ConfigurationError
                    : FromProviderCategory(providerError.Category);
            }
        }

        return StepRunErrorCategory.Unknown;
    }

    // OpenAI reports an empty account as a 429 rate limit with one of these codes. Unlike a real rate
    // limit, waiting does not help: it fails again until someone adds credit, so retrying only adds delay.
    private static bool IsOutOfCredit(AIProviderException providerError)
        => string.Equals(providerError.ProviderCode, "insufficient_quota", StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerError.ProviderCode, "credit_balance_exhausted", StringComparison.OrdinalIgnoreCase);

    private static StepRunErrorCategory FromProviderCategory(AIProviderErrorCategory category) => category switch
    {
        AIProviderErrorCategory.RateLimited => StepRunErrorCategory.RateLimiting,
        AIProviderErrorCategory.Transient => StepRunErrorCategory.ServiceUnavailable,
        AIProviderErrorCategory.NetworkError => StepRunErrorCategory.ServiceUnavailable,
        AIProviderErrorCategory.Authentication => StepRunErrorCategory.Authentication,

        // Resending the same request to the same model cannot change the answer, so these are
        // settings problems rather than transient ones.
        AIProviderErrorCategory.InvalidRequest => StepRunErrorCategory.ConfigurationError,
        AIProviderErrorCategory.NotFound => StepRunErrorCategory.ConfigurationError,

        AIProviderErrorCategory.Cancelled => StepRunErrorCategory.Cancelled,
        _ => StepRunErrorCategory.Unknown,
    };
}
