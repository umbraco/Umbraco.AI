using System.Diagnostics;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Embeddings;

namespace Umbraco.Ai.Core.Audit.Middleware;

/// <summary>
/// Embedding middleware that emits OpenTelemetry Activities for AI embedding operations.
/// Activities are captured by the ActivityListener for local persistence and can be
/// exported to external observability systems if configured by the user.
/// </summary>
public sealed class AiTelemetryEmbeddingMiddleware : IAiEmbeddingMiddleware
{
    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> Apply(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        return new TelemetryEmbeddingGenerator(generator);
    }

    private sealed class TelemetryEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
    {
        public TelemetryEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator)
            : base(innerGenerator)
        {
        }

        public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Build tags collection before creating Activity so they're available in OnActivityStarted
            // Activity tags only support primitive types, so convert Guids to strings
            var tags = new List<KeyValuePair<string, object?>>();

            if (options?.AdditionalProperties is not null)
            {
                foreach (var prop in options.AdditionalProperties)
                {
                    // Convert Guid values to string for Activity tags
                    var value = prop.Value is Guid guid ? guid.ToString() : prop.Value;
                    tags.Add(new KeyValuePair<string, object?>(prop.Key, value));
                }
            }

            if (options?.ModelId is not null)
            {
                tags.Add(new KeyValuePair<string, object?>(AiTelemetrySource.ModelIdTag, options.ModelId));
            }

            // Add input count tag
            tags.Add(new KeyValuePair<string, object?>("ai.embedding.input_count", values.Count()));

            // Create Activity with tags passed at creation time (available immediately in OnActivityStarted)
            using var activity = AiTelemetrySource.Source.StartActivity(
                AiTelemetrySource.EmbeddingRequestActivity,
                ActivityKind.Internal,
                parentContext: default,
                tags: tags.Count > 0 ? tags : null);

            // Store input values as custom property (also set synchronously before async operation)
            activity?.SetCustomProperty("Prompt", values.ToList());

            try
            {
                var result = await base.GenerateAsync(values, options, cancellationToken);

                // Set response tags
                if (activity is not null && result.Usage is not null)
                {
                    if (result.Usage.InputTokenCount.HasValue)
                    {
                        activity.SetTag(AiTelemetrySource.TokensInputTag, result.Usage.InputTokenCount.Value);
                    }

                    if (result.Usage.OutputTokenCount.HasValue)
                    {
                        activity.SetTag(AiTelemetrySource.TokensOutputTag, result.Usage.OutputTokenCount.Value);
                    }

                    if (result.Usage.TotalTokenCount.HasValue)
                    {
                        activity.SetTag(AiTelemetrySource.TokensTotalTag, result.Usage.TotalTokenCount.Value);
                    }
                }

                // Store result in Activity custom property for listener to access
                activity?.SetCustomProperty("Response", result);

                return result;
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
    }
}
