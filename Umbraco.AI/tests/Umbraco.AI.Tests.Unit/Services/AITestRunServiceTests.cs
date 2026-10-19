using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Tests.Unit.Services;

public class AITestRunServiceTests
{
    private static readonly Guid TestId = Guid.NewGuid();
    private static readonly Guid ProfileId = Guid.NewGuid();

    private readonly Mock<IAITestRunRepository> _runRepositoryMock = new();
    private readonly AITestRunService _service;

    public AITestRunServiceTests()
    {
        _service = new AITestRunService(
            _runRepositoryMock.Object,
            Mock.Of<IAITestTranscriptRepository>(),
            Mock.Of<IAITestRepository>());
    }

    private AITestRun AddRun(AITestUsage? usage, long durationMs = 1000)
    {
        var run = new AITestRun
        {
            TestId = TestId,
            Status = AITestRunStatus.Passed,
            DurationMs = durationMs,
            Outcome = new AITestOutcome { Usage = usage },
        };
        _runRepositoryMock.Setup(x => x.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);
        return run;
    }

    private static AITestUsageEntry Entry(string model, int totalTokens, long durationMs = 100, int failed = 0) => new()
    {
        Capability = AICapability.Chat,
        ProviderId = "openai",
        ModelId = model,
        ProfileId = ProfileId,
        ProfileAlias = "chat",
        FeatureType = "prompt",
        TotalTokens = totalTokens,
        DurationMs = durationMs,
        FailedCallCount = failed,
        CallCount = 1,
    };

    private static AITestUsage Usage(params AITestUsageEntry[] entries) => new()
    {
        InputTokens = entries.Sum(e => e.TotalTokens) / 2,
        OutputTokens = entries.Sum(e => e.TotalTokens) - entries.Sum(e => e.TotalTokens) / 2,
        TotalTokens = entries.Sum(e => e.TotalTokens),
        CallCount = entries.Sum(e => e.CallCount),
        FailedCallCount = entries.Sum(e => e.FailedCallCount),
        DurationMs = entries.Sum(e => e.DurationMs),
        Breakdown = [.. entries],
    };

    [Fact]
    public async Task CompareTestRunsAsync_WithUsageOnBothRuns_ReturnsUsageChanges()
    {
        // Arrange
        var baseline = AddRun(Usage(Entry("gpt-4o", 1000, durationMs: 500)));
        var comparison = AddRun(Usage(Entry("gpt-4o", 600, durationMs: 300, failed: 1)));

        // Act
        var result = await _service.CompareTestRunsAsync(baseline.Id, comparison.Id);

        // Assert
        var usage = result.UsageComparison.ShouldNotBeNull();
        usage.TotalTokensChange.ShouldBe(-400);
        usage.InputTokensChange.ShouldBe(-200);
        usage.OutputTokensChange.ShouldBe(-200);
        usage.CallDurationChangeMs.ShouldBe(-200);
        usage.FailedCallCountChange.ShouldBe(1);
        usage.BreakdownChanged.ShouldBeFalse();
        usage.Entries.ShouldHaveSingleItem().TotalTokensChange.ShouldBe(-400);
    }

    [Fact]
    public async Task CompareTestRunsAsync_WhenModelChanged_ShowsEachModelOnItsOwnSide()
    {
        // Arrange
        var baseline = AddRun(Usage(Entry("gpt-4o", 1000)));
        var comparison = AddRun(Usage(Entry("gpt-4o-mini", 800)));

        // Act
        var result = await _service.CompareTestRunsAsync(baseline.Id, comparison.Id);

        // Assert
        var usage = result.UsageComparison.ShouldNotBeNull();
        usage.BreakdownChanged.ShouldBeTrue();
        usage.Entries.Count.ShouldBe(2);

        var removed = usage.Entries.Single(e => e.ModelId == "gpt-4o");
        removed.ComparisonEntry.ShouldBeNull();
        removed.TotalTokensChange.ShouldBe(-1000);

        var added = usage.Entries.Single(e => e.ModelId == "gpt-4o-mini");
        added.BaselineEntry.ShouldBeNull();
        added.TotalTokensChange.ShouldBe(800);
    }

    [Fact]
    public async Task CompareTestRunsAsync_WhenProfileRenamed_MatchesTheSameEntry()
    {
        // Arrange
        var renamed = Entry("gpt-4o", 900);
        renamed.ProfileAlias = "chat-renamed";
        var baseline = AddRun(Usage(Entry("gpt-4o", 1000)));
        var comparison = AddRun(Usage(renamed));

        // Act
        var result = await _service.CompareTestRunsAsync(baseline.Id, comparison.Id);

        // Assert
        var usage = result.UsageComparison.ShouldNotBeNull();
        usage.BreakdownChanged.ShouldBeFalse();
        usage.Entries.ShouldHaveSingleItem().ProfileAlias.ShouldBe("chat-renamed");
    }

    [Fact]
    public async Task CompareTestRunsAsync_WhenEitherRunHasNoUsage_ReturnsNullUsageComparison()
    {
        // Arrange
        var withUsage = AddRun(Usage(Entry("gpt-4o", 1000)));
        var withoutUsage = AddRun(usage: null);

        // Act
        var result = await _service.CompareTestRunsAsync(withoutUsage.Id, withUsage.Id);

        // Assert
        result.UsageComparison.ShouldBeNull();
    }

    [Fact]
    public async Task CompareTestRunsAsync_WhenRunHasUnreportedCalls_FlagsTokenChangesAsApproximate()
    {
        // Arrange
        var unreported = Usage(Entry("gpt-4o", 500));
        unreported.UnreportedCallCount = 1;
        var baseline = AddRun(Usage(Entry("gpt-4o", 1000)));
        var comparison = AddRun(unreported);

        // Act
        var result = await _service.CompareTestRunsAsync(baseline.Id, comparison.Id);

        // Assert
        result.UsageComparison.ShouldNotBeNull().HasUnreportedCalls.ShouldBeTrue();
    }
}
