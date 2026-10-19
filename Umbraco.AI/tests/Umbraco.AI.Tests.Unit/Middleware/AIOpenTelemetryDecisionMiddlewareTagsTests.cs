#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Diagnostics;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

/// <summary>
/// #562: a decision call's gen_ai span carries the tracked call's tags, like Chat's does.
/// </summary>
public class AIOpenTelemetryDecisionMiddlewareTagsTests
{
    /// <summary>Filter the process-wide listener to this middleware's span (see otel-test-shared-activity-source).</summary>
    private const string DecisionSpanName = "gen_ai.decision";

    [Fact]
    public async Task GetResponseAsync_TagsTheGenAiSpan()
    {
        // Arrange
        Activity? captured = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AITelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == DecisionSpanName)
                {
                    captured = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var client = new AIOpenTelemetryDecisionMiddleware().Apply(new FakeDecisionClient());
        var tags = new Dictionary<string, string> { [AITelemetry.Tags.ProfileAlias] = "decision-profile" };

        // Act
        using (AITraceTags.Enter(tags))
        {
            await client.GetResponseAsync(new AIDecisionRequest
            {
                State = "text",
                Questions = [new AIBinaryDecisionQuestion { Id = "spam", Instructions = "Is this spam?" }],
            });
        }

        // Assert
        captured.ShouldNotBeNull();
        captured.Source.Name.ShouldBe(AITelemetry.SourceName);
        captured.GetTagItem(AITelemetry.Tags.ProfileAlias).ShouldBe("decision-profile");
    }

    [Fact]
    public async Task GetResponseAsync_NestedCallWithNoTags_DoesNotShowItsParentsTags()
    {
        // Arrange
        Activity? captured = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AITelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == DecisionSpanName)
                {
                    captured = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var client = new AIOpenTelemetryDecisionMiddleware().Apply(new FakeDecisionClient());

        // Act
        using (AITraceTags.Enter(new Dictionary<string, string> { [AITelemetry.Tags.ProfileAlias] = "parent" }))
        using (AITraceTags.Enter(new Dictionary<string, string>()))
        {
            await client.GetResponseAsync(new AIDecisionRequest
            {
                State = "text",
                Questions = [new AIBinaryDecisionQuestion { Id = "spam", Instructions = "Is this spam?" }],
            });
        }

        // Assert
        captured.ShouldNotBeNull();
        captured.GetTagItem(AITelemetry.Tags.ProfileAlias).ShouldBeNull();
    }
}
