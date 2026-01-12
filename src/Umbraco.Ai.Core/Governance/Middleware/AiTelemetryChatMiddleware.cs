using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;

namespace Umbraco.Ai.Core.Governance.Middleware;

/// <summary>
/// Chat middleware that emits OpenTelemetry Activities for AI operations.
/// Activities are captured by the ActivityListener for local persistence and can be
/// exported to external observability systems if configured by the user.
/// </summary>
public sealed class AiTelemetryChatMiddleware : IAiChatMiddleware
{
    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new TelemetryChatClient(client);
    }

    private sealed class TelemetryChatClient : IChatClient
    {
        private readonly IChatClient _innerClient;

        public TelemetryChatClient(IChatClient innerClient)
        {
            _innerClient = innerClient;
        }

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var activity = AiTelemetrySource.Source.StartActivity(
                AiTelemetrySource.ChatRequestActivity,
                ActivityKind.Internal);

            if (activity is not null)
            {
                // Set tags from options.AdditionalProperties
                if (options?.AdditionalProperties is not null)
                {
                    foreach (var prop in options.AdditionalProperties)
                    {
                        activity.SetTag(prop.Key, prop.Value?.ToString());
                    }
                }

                // Set standard tags
                if (options?.ModelId is not null)
                {
                    activity.SetTag(AiTelemetrySource.ModelIdTag, options.ModelId);
                }
            }

            try
            {
                var response = await _innerClient.GetResponseAsync(chatMessages, options, cancellationToken);

                // Set response tags
                if (activity is not null && response.Usage is not null)
                {
                    if (response.Usage.InputTokenCount.HasValue)
                    {
                        activity.SetTag(AiTelemetrySource.TokensInputTag, response.Usage.InputTokenCount.Value);
                    }

                    if (response.Usage.OutputTokenCount.HasValue)
                    {
                        activity.SetTag(AiTelemetrySource.TokensOutputTag, response.Usage.OutputTokenCount.Value);
                    }

                    if (response.Usage.TotalTokenCount.HasValue)
                    {
                        activity.SetTag(AiTelemetrySource.TokensTotalTag, response.Usage.TotalTokenCount.Value);
                    }
                }

                // Store response in Activity custom property for listener to access
                activity?.SetCustomProperty("Response", response);

                return response;
            }
            catch (Exception ex)
            {
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.AddEvent(new ActivityEvent("exception",
                        tags: new ActivityTagsCollection
                        {
                            { "exception.type", ex.GetType().FullName },
                            { "exception.message", ex.Message }
                        }));
                }

                throw;
            }
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var activity = AiTelemetrySource.Source.StartActivity(
                AiTelemetrySource.ChatRequestActivity,
                ActivityKind.Internal);

            if (activity is not null)
            {
                // Set tags from options.AdditionalProperties
                if (options?.AdditionalProperties is not null)
                {
                    foreach (var prop in options.AdditionalProperties)
                    {
                        activity.SetTag(prop.Key, prop.Value?.ToString());
                    }
                }

                // Set standard tags
                if (options?.ModelId is not null)
                {
                    activity.SetTag(AiTelemetrySource.ModelIdTag, options.ModelId);
                }
            }

            IAsyncEnumerable<ChatResponseUpdate> stream;

            try
            {
                stream = _innerClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
            }
            catch (Exception ex)
            {
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.AddEvent(new ActivityEvent("exception",
                        tags: new ActivityTagsCollection
                        {
                            { "exception.type", ex.GetType().FullName },
                            { "exception.message", ex.Message }
                        }));
                }

                throw;
            }

            // Stream updates without detailed token tracking for now
            // Token tracking for streaming can be added based on specific provider implementations
            await foreach (var update in stream.WithCancellation(cancellationToken))
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? key = null)
        {
            return _innerClient.GetService(serviceType, key);
        }

        public void Dispose()
        {
            _innerClient.Dispose();
        }
    }
}
