using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Persistence.Tests;

namespace Umbraco.AI.Tests.Unit.Factories;

public class AITestRunFactoryTests
{
    public class GivenARunWithUsage
    {
        private readonly AITestRunEntity _entity;
        private readonly AITestRun _loaded;

        public GivenARunWithUsage()
        {
            var run = new AITestRun
            {
                TestId = Guid.NewGuid(),
                Outcome = new AITestOutcome
                {
                    Usage = new AITestUsage
                    {
                        InputTokens = 10,
                        OutputTokens = 5,
                        TotalTokens = 15,
                        CallCount = 2,
                        FailedCallCount = 1,
                        DurationMs = 700,
                        Breakdown = [new AITestUsageEntry { Capability = AICapability.Chat, ModelId = "gpt-x", CallCount = 2 }]
                    }
                }
            };

            _entity = AITestRunFactory.BuildEntity(run);
            _loaded = AITestRunFactory.BuildDomain(_entity);
        }

        [Fact]
        public void StoresTheUsageInTheUsageColumn() => _entity.OutcomeUsageJson.ShouldNotBeNullOrEmpty();

        [Fact]
        public void RoundTripsTheTotals() => _loaded.Outcome!.Usage!.TotalTokens.ShouldBe(15);

        [Fact]
        public void RoundTripsTheDuration() => _loaded.Outcome!.Usage!.DurationMs.ShouldBe(700);

        [Fact]
        public void RoundTripsTheBreakdown() => _loaded.Outcome!.Usage!.Breakdown.Single().ModelId.ShouldBe("gpt-x");
    }

    public class GivenARunWithoutUsage
    {
        private readonly AITestRunEntity _entity = AITestRunFactory.BuildEntity(
            new AITestRun { TestId = Guid.NewGuid(), Outcome = new AITestOutcome() });

        [Fact]
        public void StoresNoUsage() => _entity.OutcomeUsageJson.ShouldBeNull();
    }
}
