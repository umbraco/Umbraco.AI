using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Tests.Unit.Analytics;

/// <summary>
/// #564: usage keeps which calls were nested in another tracked call (a guardrail judge, a search
/// embedding), so a feature's runs can be counted as its requests minus its nested requests.
/// </summary>
public class AIUsageNestedCallTests
{
    private static readonly DateTime Hour = new(2026, 10, 8, 13, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnalyticsRecorder_RecordsWhetherTheCallWasNested(bool isNested)
    {
        // Arrange
        var options = Mock.Of<IOptionsMonitor<AIAnalyticsOptions>>(x => x.CurrentValue == new AIAnalyticsOptions { Enabled = true });
        var recorded = new TaskCompletionSource<AIUsageRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recordingService = new Mock<IAIUsageRecordingService>();
        recordingService
            .Setup(x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()))
            .Callback((AIUsageRecord record, CancellationToken _) => recorded.TrySetResult(record))
            .Returns(ValueTask.CompletedTask);
        var recorder = new AIAnalyticsOperationRecorder(
            recordingService.Object,
            new AIUsageRecordFactory(options, Mock.Of<IBackOfficeSecurityAccessor>(), NullLogger<AIUsageRecordFactory>.Instance),
            options,
            NullLogger<AIAnalyticsOperationRecorder>.Instance);
        var start = new AIOperationStart(
            new AIOperationDescriptor { Capability = AICapability.Chat },
            new AIUsageContext
            {
                Capability = AICapability.Chat,
                ProfileId = Guid.NewGuid(),
                ProfileAlias = "profile",
                ProviderId = "openai",
                ModelId = "gpt",
            },
            LogValues: null,
            IsNested: isNested);

        // Act
        var recording = await recorder.BeginAsync(start, CancellationToken.None);
        await recording!.EndAsync(new AIOperationOutcome(AIOperationStatus.Succeeded, Usage: null, DurationMs: 5, Exception: null));

        // Assert
        (await recorded.Task.WaitAsync(TimeSpan.FromSeconds(2))).IsNested.ShouldBe(isNested);
    }

    [Fact]
    public void GroupRecords_CountsTheNestedRequests()
    {
        // Act
        var statistics = AIUsageStatisticsGrouping.GroupRecords(
            [Record(isNested: false), Record(isNested: true), Record(isNested: true)],
            _ => Hour);

        // Assert
        statistics.Select(s => (s.RequestCount, s.NestedRequestCount)).ShouldBe([(3, 2)]);
    }

    [Fact]
    public void GroupStatistics_AddsUpTheNestedRequests()
    {
        // Act
        var statistics = AIUsageStatisticsGrouping.GroupStatistics(
            [Statistics(Hour, requests: 4, nested: 1), Statistics(Hour.AddHours(1), requests: 6, nested: 2)],
            period => period.Date);

        // Assert
        statistics.Select(s => (s.RequestCount, s.NestedRequestCount)).ShouldBe([(10, 3)]);
    }

    [Fact]
    public void Summary_TopLevelRequests_LeaveOutNestedOnes()
    {
        // Arrange
        var summary = new AIUsageSummary
        {
            TotalRequests = 10,
            NestedRequestCount = 4,
            InputTokens = 0,
            OutputTokens = 0,
            TotalTokens = 0,
            SuccessCount = 10,
            FailureCount = 0,
            SuccessRate = 1,
            AverageDurationMs = 0,
        };

        // Act + Assert
        summary.TopLevelRequestCount.ShouldBe(6);
    }

    private static AIUsageRecord Record(bool isNested) => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = Hour.AddMinutes(10),
        Capability = AICapability.Chat,
        ProfileId = Guid.Empty,
        ProfileAlias = "profile",
        ProviderId = "openai",
        ModelId = "gpt",
        InputTokens = 1,
        OutputTokens = 1,
        TotalTokens = 2,
        DurationMs = 10,
        Status = AIUsageRecordStatus.Succeeded,
        IsNested = isNested,
        CreatedAt = Hour,
    };

    private static AIUsageStatistics Statistics(DateTime period, int requests, int nested) => new()
    {
        Id = Guid.NewGuid(),
        Period = period,
        ProviderId = "openai",
        ModelId = "gpt",
        ProfileId = Guid.Empty,
        ProfileAlias = "profile",
        Capability = AICapability.Chat,
        RequestCount = requests,
        NestedRequestCount = nested,
        SuccessCount = requests,
        FailureCount = 0,
        InputTokens = requests,
        OutputTokens = requests,
        TotalTokens = requests * 2,
        TotalDurationMs = requests * 10,
        CreatedAt = period,
    };
}
