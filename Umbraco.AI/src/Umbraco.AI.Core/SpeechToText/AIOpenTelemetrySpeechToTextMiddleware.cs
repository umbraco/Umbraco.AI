using System.Diagnostics;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Telemetry;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Speech-to-text middleware that adds OpenTelemetry tracing and metrics.
/// Creates a <c>gen_ai.speech_to_text</c> span with Umbraco.AI as the source.
/// </summary>
/// <remarks>
/// <para>
/// This middleware has zero overhead when no OpenTelemetry listener is configured.
/// It is registered as the innermost middleware so that <c>Activity.Current</c> is
/// available to all outer middleware for enrichment.
/// </para>
/// <para>
/// Unlike Chat and Embedding, M.E.AI does not yet provide a built-in OpenTelemetry
/// wrapper for <see cref="ISpeechToTextClient"/>. This middleware creates spans manually
/// using the same source name for consistency.
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
public sealed class AIOpenTelemetrySpeechToTextMiddleware : IAISpeechToTextMiddleware
{
    private static readonly ActivitySource ActivitySource = new(AITelemetry.SourceName);

    /// <inheritdoc />
    public ISpeechToTextClient Apply(ISpeechToTextClient client)
    {
        return new AIOpenTelemetrySpeechToTextClient(client);
    }

    private sealed class AIOpenTelemetrySpeechToTextClient : AIBoundSpeechToTextClientBase
    {
        public AIOpenTelemetrySpeechToTextClient(ISpeechToTextClient innerClient)
            : base(innerClient)
        {
        }

        public override async Task<SpeechToTextResponse> GetTextAsync(
            Stream audioSpeechStream,
            SpeechToTextOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity("gen_ai.speech_to_text");

            if (activity is not null)
            {
                EnrichActivity(activity, options);
            }

            try
            {
                var response = await base.GetTextAsync(audioSpeechStream, options, cancellationToken);

                if (activity is not null)
                {
                    activity.SetTag("gen_ai.response.text_length", response.Text?.Length ?? 0);
                }

                return response;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        public override async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
            Stream audioSpeechStream,
            SpeechToTextOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity("gen_ai.speech_to_text");

            if (activity is not null)
            {
                EnrichActivity(activity, options);
                activity.SetTag("gen_ai.streaming", true);
            }

            IAsyncEnumerable<SpeechToTextResponseUpdate> stream;

            try
            {
                stream = base.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }

            await foreach (var update in stream)
            {
                yield return update;
            }
        }

        private static void EnrichActivity(Activity activity, SpeechToTextOptions? options)
        {
            activity.SetTag("gen_ai.operation.name", "speech_to_text");

            if (options?.ModelId is not null)
            {
                activity.SetTag("gen_ai.request.model", options.ModelId);
            }

            if (options?.SpeechLanguage is not null)
            {
                activity.SetTag("gen_ai.request.language", options.SpeechLanguage);
            }
        }
    }
}
