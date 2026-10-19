using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Internal outcome wrapper returned by a tracked operation: the result for the caller, plus what the
/// tracker hands to its recorders.
/// </summary>
internal sealed class AITrackedOperationResult<TResult>
{
    public required TResult Result { get; init; }

    /// <summary>Token usage reported by the provider (nullable; speech-to-text has none).</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>
    /// What the call returned, in a form worth keeping (chat messages, embeddings, transcribed text, an
    /// image count). Recorders decide what to do with it; the audit log stores it as the response.
    /// </summary>
    public object? ResponseData { get; init; }

    /// <summary>
    /// Set when the call returned normally but its result is a failure, for example a response that ends
    /// on a provider error. The caller still gets <see cref="Result"/>; recorders see a failed call.
    /// </summary>
    public Exception? Failure { get; init; }
}
