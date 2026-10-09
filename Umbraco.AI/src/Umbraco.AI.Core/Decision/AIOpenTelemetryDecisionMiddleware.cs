using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Telemetry;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Decision middleware that adds OpenTelemetry tracing and metrics.
/// Creates a <c>gen_ai.decision</c> span with Umbraco.AI as the source.
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
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIOpenTelemetryDecisionMiddleware : IAIDecisionMiddleware
{
    private static readonly ActivitySource ActivitySource = new(AITelemetry.SourceName);

    /// <inheritdoc />
    public IAIDecisionClient Apply(IAIDecisionClient client)
    {
        return new AIOpenTelemetryDecisionClient(client);
    }

    private sealed class AIOpenTelemetryDecisionClient : IAIDecisionClient
    {
        private readonly IAIDecisionClient _innerClient;

        public AIOpenTelemetryDecisionClient(IAIDecisionClient innerClient)
        {
            _innerClient = innerClient;
        }

        public async Task<AIDecisionResponse> GetResponseAsync(
            AIDecisionRequest request,
            AIDecisionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity("gen_ai.decision");

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

        private static string QuestionKind(AIDecisionQuestion question) => question switch
        {
            AIBinaryDecisionQuestion => "binary",
            AIChoiceDecisionQuestion => "choice",
            AIScoreDecisionQuestion => "score",
            _ => question.GetType().Name,
        };

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceType == GetType())
            {
                return this;
            }

            return _innerClient.GetService(serviceType, serviceKey);
        }

        public void Dispose() => _innerClient.Dispose();

        private static void EnrichActivity(Activity activity, AIDecisionRequest request, AIDecisionOptions? options)
        {
            activity.SetTag("gen_ai.operation.name", "decision");
            activity.SetTag("gen_ai.decision.question_count", request.Questions.Count);

            // Distinct kinds, in question order, comma-joined (e.g. "binary,score") — not one tag per
            // question, since gen_ai.response.confidence (per-answer, and binary has none) is dropped
            // entirely rather than tagged per question.
            var kinds = request.Questions.Select(QuestionKind).Distinct();
            activity.SetTag("gen_ai.request.kind", string.Join(",", kinds));

            if (options?.ModelId is not null)
            {
                activity.SetTag("gen_ai.request.model", options.ModelId);
            }
        }
    }
}
