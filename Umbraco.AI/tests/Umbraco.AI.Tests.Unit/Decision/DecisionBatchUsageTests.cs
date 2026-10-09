#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability surface

// DR-14 — Ask several questions in one call from C# (AC2): one model call, one usage record.

using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Decision;

public class DecisionBatchUsageTests
{
    #region Happy path

    public class GivenAThreeQuestionRequestThroughTheRealPipeline
    {
        [Fact]
        public async Task WritesExactlyOneUsageRecord()
        {
            var harness = await DecisionTrackingAndChecksHarness.CreateAsync(new FakeDecisionClient(_ => new AIDecisionResponse
            {
                Answers = new Dictionary<string, AIDecisionAnswer>
                {
                    ["a"] = new AIBinaryDecisionAnswer { TrueProbability = 0.9 },
                    ["b"] = new AIBinaryDecisionAnswer { TrueProbability = 0.1 },
                    ["c"] = new AIBinaryDecisionAnswer { TrueProbability = 0.5 },
                },
            }));

            await harness.Client.GetResponseAsync(new AIDecisionRequest
            {
                State = "text",
                Questions =
                [
                    new AIBinaryDecisionQuestion { Id = "a", Instructions = "A?" },
                    new AIBinaryDecisionQuestion { Id = "b", Instructions = "B?" },
                    new AIBinaryDecisionQuestion { Id = "c", Instructions = "C?" },
                ],
            });

            harness.UsageRecordingServiceMock.Verify(
                x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    #endregion
}
