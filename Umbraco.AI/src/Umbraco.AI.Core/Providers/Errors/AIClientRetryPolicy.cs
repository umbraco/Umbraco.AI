using System.ClientModel.Primitives;
using System.Net.Http;

namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// The retry policy for clients built on <c>System.ClientModel</c>, such as the OpenAI SDK and the
/// providers that reuse it for an OpenAI-compatible API.
/// </summary>
/// <remarks>
/// It retries exactly like <see cref="ClientRetryPolicy"/>, the default for an
/// <c>OpenAIClient</c>, except for a reply that says the account is out of credit (HTTP 429 with code
/// <c>insufficient_quota</c>). The default treats every 429 as a rate limit and retries it with
/// backoff, so an empty account kept the user waiting for an error that was certain from the first
/// reply. Set it on the client's options:
/// <code>new OpenAIClientOptions { RetryPolicy = AIClientRetryPolicy.Default }</code>
/// Don't set it on a client whose SDK brings its own retry policy, such as <c>AzureOpenAIClient</c>,
/// which waits for Azure's <c>retry-after-ms</c> header. This policy would replace that.
/// </remarks>
public sealed class AIClientRetryPolicy : ClientRetryPolicy
{
    /// <summary>
    /// A shared instance with the SDK's default number of retries. The policy holds no per-request
    /// state, so one instance serves every client.
    /// </summary>
    public static AIClientRetryPolicy Default { get; } = new();

    /// <summary>
    /// Creates the policy.
    /// </summary>
    /// <param name="maxRetries">The most retries for one request.</param>
    public AIClientRetryPolicy(int maxRetries = 3)
        : base(maxRetries)
    {
    }

    /// <inheritdoc />
    protected override bool ShouldRetry(PipelineMessage message, Exception? exception)
    {
        if (message.Response is { Status: 429 } response)
        {
            // A streamed request leaves the body unread. An error body is small, and the SDK reads it
            // anyway to build its exception. If it can't be read, retry as the default would.
            try
            {
                response.BufferContent(message.CancellationToken);
                if (IsOutOfCredit(response))
                {
                    return false;
                }
            }
            catch (Exception ex) when (ex is IOException or HttpRequestException)
            {
            }
        }

        return base.ShouldRetry(message, exception);
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> ShouldRetryAsync(PipelineMessage message, Exception? exception)
    {
        if (message.Response is { Status: 429 } response)
        {
            try
            {
                await response.BufferContentAsync(message.CancellationToken).ConfigureAwait(false);
                if (IsOutOfCredit(response))
                {
                    return false;
                }
            }
            catch (Exception ex) when (ex is IOException or HttpRequestException)
            {
            }
        }

        return await base.ShouldRetryAsync(message, exception).ConfigureAwait(false);
    }

    private static bool IsOutOfCredit(PipelineResponse response)
        => ProviderErrorMapping.IsOutOfCredit(response.Status, ProviderErrorMapping.TryExtractOpenAIErrorCode(response.Content));
}
