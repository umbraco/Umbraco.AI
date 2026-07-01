using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Telemetry;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Wraps the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Image-generation middleware that adds OpenTelemetry tracing and metrics.
/// Creates a <c>gen_ai.image_generation</c> span with Umbraco.AI as the source.
/// </summary>
/// <remarks>
/// <para>
/// This middleware has zero overhead when no OpenTelemetry listener is configured.
/// It is registered as the innermost middleware so that <c>Activity.Current</c> is
/// available to all outer middleware for enrichment.
/// </para>
/// <para>
/// M.E.AI does not yet provide a built-in OpenTelemetry wrapper for <see cref="IImageGenerator"/>.
/// This middleware creates spans manually using the same source name for consistency.
/// </para>
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public sealed class AIOpenTelemetryImageGenerationMiddleware : IAIImageGenerationMiddleware
{
    private static readonly ActivitySource ActivitySource = new(AITelemetry.SourceName);

    /// <inheritdoc />
    public IImageGenerator Apply(IImageGenerator generator)
    {
        return new AIOpenTelemetryImageGenerator(generator);
    }

    private sealed class AIOpenTelemetryImageGenerator : AIBoundImageGeneratorBase
    {
        public AIOpenTelemetryImageGenerator(IImageGenerator innerGenerator)
            : base(innerGenerator)
        {
        }

        public override async Task<ImageGenerationResponse> GenerateAsync(
            ImageGenerationRequest request,
            ImageGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity("gen_ai.image_generation");

            if (activity is not null)
            {
                EnrichActivity(activity, request, options);
            }

            try
            {
                var response = await base.GenerateAsync(request, options, cancellationToken);

                if (activity is not null)
                {
                    activity.SetTag("gen_ai.response.image_count", response.Contents?.Count ?? 0);
                }

                return response;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        private static void EnrichActivity(Activity activity, ImageGenerationRequest request, ImageGenerationOptions? options)
        {
            activity.SetTag("gen_ai.operation.name", "image_generation");

            if (options?.ModelId is not null)
            {
                activity.SetTag("gen_ai.request.model", options.ModelId);
            }

            if (options?.Count is { } count)
            {
                activity.SetTag("gen_ai.request.image_count", count);
            }

            if (options?.ImageSize is { } size)
            {
                activity.SetTag("gen_ai.request.image_size", $"{size.Width}x{size.Height}");
            }

            if (request.OriginalImages is not null)
            {
                activity.SetTag("gen_ai.request.is_edit", true);
            }
        }
    }
}
