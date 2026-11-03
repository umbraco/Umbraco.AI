using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Telemetry;

#pragma warning disable UMBRACOAI_EXTRACTION // IAIExtractionClient is experimental

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Extraction middleware that adds OpenTelemetry tracing and metrics.
/// Creates a <c>gen_ai.extraction</c> span with Umbraco.AI as the source.
/// </summary>
/// <remarks>
/// <para>
/// This middleware has zero overhead when no OpenTelemetry listener is configured.
/// It is registered as the innermost middleware so that <c>Activity.Current</c> is
/// available to all outer middleware for enrichment, mirroring
/// <c>AIOpenTelemetrySpeechToTextMiddleware</c>.
/// </para>
/// <para>
/// Users opt in to collecting telemetry by adding the source name to their
/// OpenTelemetry configuration:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(AITelemetry.SourceName))
///     .WithMetrics(m => m.AddMeter(AITelemetry.SourceName));
/// </code>
/// </para>
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIOpenTelemetryExtractionMiddleware : IAIExtractionMiddleware
{
    private static readonly ActivitySource ActivitySource = new(AITelemetry.SourceName);

    /// <inheritdoc />
    public IAIExtractionClient Apply(IAIExtractionClient client)
    {
        return new AIOpenTelemetryExtractionClient(client);
    }

    private sealed class AIOpenTelemetryExtractionClient : IAIExtractionClient
    {
        private readonly IAIExtractionClient _innerClient;

        public AIOpenTelemetryExtractionClient(IAIExtractionClient innerClient)
        {
            _innerClient = innerClient;
        }

        public async Task<AIExtractionResponse> GetResponseAsync(
            AIExtractionRequest request,
            AIExtractionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity("gen_ai.extraction");

            if (activity is not null)
            {
                EnrichActivity(activity, request, options);
            }

            try
            {
                return await _innerClient.GetResponseAsync(request, options, cancellationToken);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceType == GetType())
            {
                return this;
            }

            return _innerClient.GetService(serviceType, serviceKey);
        }

        public void Dispose() => _innerClient.Dispose();

        private static void EnrichActivity(Activity activity, AIExtractionRequest request, AIExtractionOptions? options)
        {
            activity.SetTag("gen_ai.operation.name", "extraction");
            AITraceTags.Apply(activity);
            activity.SetTag("gen_ai.extraction.field_count", request.Fields.Count);

            // Distinct kinds, in field order, comma-joined (e.g. "text,date") — not one tag per field.
            var kinds = request.Fields.Select(ExtractionFieldKind.Of).Distinct();
            activity.SetTag("gen_ai.request.kind", string.Join(",", kinds));

            if (options?.ModelId is not null)
            {
                activity.SetTag("gen_ai.request.model", options.ModelId);
            }
        }
    }
}
