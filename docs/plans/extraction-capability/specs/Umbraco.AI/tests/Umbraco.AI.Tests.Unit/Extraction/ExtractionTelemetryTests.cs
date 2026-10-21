#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-8 — Extraction calls are tracked like other capabilities (AC7): one OpenTelemetry span with the shared tags.
//
// ASSUMPTION (builder confirms/adjusts): the span is named "gen_ai.extraction", like Decision's "gen_ai.decision".

using System.Diagnostics;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// <see cref="AIOpenTelemetryExtractionMiddleware"/> wrapped around a fake client, listened to the same way as
/// <c>AIOpenTelemetryDecisionMiddlewareTagsTests</c>.
/// </summary>
public class ExtractionTelemetryTests
{
    /// <summary>Filter the process-wide listener to this middleware's span (see otel-test-shared-activity-source).</summary>
    private const string ExtractionSpanName = "gen_ai.extraction";

    #region Happy path

    public class GivenAListenerAndATracedProfileAlias
    {
        private readonly List<Activity> _spans = [];

        public GivenAListenerAndATracedProfileAlias()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == AITelemetry.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (activity.OperationName == ExtractionSpanName)
                    {
                        _spans.Add(activity);
                    }
                },
            };
            ActivitySource.AddActivityListener(listener);

            var client = new AIOpenTelemetryExtractionMiddleware().Apply(new FakeExtractionClient());
            var tags = new Dictionary<string, string> { [AITelemetry.Tags.ProfileAlias] = "cloudflare-clairic" };

            using (AITraceTags.Enter(tags))
            {
                client.GetResponseAsync(new AIExtractionRequest
                {
                    State = "Order A-88412",
                    Fields = [new AITextExtractionField { Id = "orderId", Instructions = "The order number" }],
                }).GetAwaiter().GetResult();
            }
        }

        [Fact(Skip = "Pending")]
        public void EmitsExactlyOneSpan() => _spans.Count.ShouldBe(1);

        [Fact(Skip = "Pending")]
        public void TagsTheSpanWithTheSharedTraceTags()
            => _spans.Single().GetTagItem(AITelemetry.Tags.ProfileAlias).ShouldBe("cloudflare-clairic");
    }

    #endregion
}
