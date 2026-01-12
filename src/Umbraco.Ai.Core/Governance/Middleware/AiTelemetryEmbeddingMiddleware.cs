using System.Diagnostics;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Embeddings;

namespace Umbraco.Ai.Core.Governance.Middleware;

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
            using var activity = AiTelemetrySource.Source.StartActivity(
                AiTelemetrySource.EmbeddingRequestActivity,
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

                // Set input count
                activity.SetTag("ai.embedding.input_count", values.Count());
            }

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
