using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Tests.Unit.Analytics;

/// <summary>
/// A call stopped by a guardrail is stored as "Blocked" in usage analytics, matching the audit log, and
/// still counts as a failure in totals so the dashboard's numbers don't change.
/// </summary>
public class AIUsageBlockedStatusTests
{
    [Theory]
    [InlineData(true, false, AIUsageRecordStatus.Succeeded)]
    [InlineData(false, false, AIUsageRecordStatus.Failed)]
    [InlineData(false, true, AIUsageRecordStatus.Blocked)]
    public void RecordFactory_StoresTheOutcomeAsStatus(bool succeeded, bool blocked, AIUsageRecordStatus expected)
    {
        var options = new Mock<IOptionsMonitor<AIAnalyticsOptions>>();
        options.Setup(x => x.CurrentValue).Returns(new AIAnalyticsOptions());
        var factory = new AIUsageRecordFactory(
            options.Object, Mock.Of<IBackOfficeSecurityAccessor>(), NullLogger<AIUsageRecordFactory>.Instance);

        var record = factory.Create(
            new AIUsageRecordContext
            {
                Capability = AICapability.Chat,
                ProfileId = Guid.NewGuid(),
                ProfileAlias = "profile",
                ProviderId = "openai",
                ModelId = "gpt",
            },
            new AIUsageRecordResult { DurationMs = 1, Succeeded = succeeded, Blocked = blocked });

        record.Status.ShouldBe(expected);
    }

    [Theory]
    [InlineData(AIUsageRecordStatus.Succeeded)]
    [InlineData(AIUsageRecordStatus.Failed)]
    [InlineData(AIUsageRecordStatus.Blocked)]
    public void Persistence_StoresTheStatusByName_AndReadsItBack(AIUsageRecordStatus status)
    {
        var entity = Umbraco.AI.Persistence.Analytics.Usage.AIUsageRecordFactory.BuildUsageRecordEntity(
            Record(DateTime.UtcNow, status));

        entity.Status.ShouldBe(status.ToString());
        Umbraco.AI.Persistence.Analytics.Usage.AIUsageRecordFactory.BuildUsageRecordDomain(entity).Status.ShouldBe(status);
    }

    [Theory]
    [InlineData("Something")]
    [InlineData("7")]
    public void Persistence_ReadsAnUnrecognisedStatusAsFailed(string stored)
    {
        var entity = Umbraco.AI.Persistence.Analytics.Usage.AIUsageRecordFactory.BuildUsageRecordEntity(
            Record(DateTime.UtcNow, AIUsageRecordStatus.Succeeded));
        entity.Status = stored;

        Umbraco.AI.Persistence.Analytics.Usage.AIUsageRecordFactory.BuildUsageRecordDomain(entity).Status
            .ShouldBe(AIUsageRecordStatus.Failed);
    }

    [Fact]
    public async Task HourlyAggregation_CountsBlockedAsAFailure()
    {
        // Arrange
        var hour = new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);
        var records = new Mock<IAIUsageRecordRepository>();
        records
            .Setup(x => x.GetRecordsByPeriodAsync(hour, hour.AddHours(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Record(hour, AIUsageRecordStatus.Succeeded), Record(hour, AIUsageRecordStatus.Failed), Record(hour, AIUsageRecordStatus.Blocked)]);
        var statistics = new Mock<IAIUsageStatisticsRepository>();
        List<AIUsageStatistics> saved = [];
        statistics
            .Setup(x => x.SaveHourlyBatchAsync(It.IsAny<IEnumerable<AIUsageStatistics>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<AIUsageStatistics>, CancellationToken>((s, _) => saved.AddRange(s))
            .Returns(Task.CompletedTask);
        var service = new AIUsageAggregationService(
            records.Object, statistics.Object, NullLogger<AIUsageAggregationService>.Instance);

        // Act
        await service.AggregateHourlyAsync(hour);

        // Assert
        var row = saved.ShouldHaveSingleItem();
        row.RequestCount.ShouldBe(3);
        row.SuccessCount.ShouldBe(1);
        row.FailureCount.ShouldBe(2);
    }

    [Fact]
    public async Task LiveStatistics_CountBlockedAsAFailure()
    {
        // Arrange: no aggregation yet, so the dashboard reads the raw records.
        var now = DateTime.UtcNow;
        var records = new Mock<IAIUsageRecordRepository>();
        records
            .Setup(x => x.GetRecordsByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Record(now.AddMinutes(-2), AIUsageRecordStatus.Succeeded), Record(now.AddMinutes(-1), AIUsageRecordStatus.Blocked)]);
        var service = new AIUsageAnalyticsService(
            records.Object, Mock.Of<IAIUsageStatisticsRepository>(), NullLogger<AIUsageAnalyticsService>.Instance);

        // Act
        var summary = await service.GetSummaryAsync(now.AddHours(-1), now.AddMinutes(1), AIUsagePeriod.Hourly);

        // Assert
        summary.TotalRequests.ShouldBe(2);
        summary.SuccessCount.ShouldBe(1);
        summary.FailureCount.ShouldBe(1);
    }

    private static AIUsageRecord Record(DateTime timestamp, AIUsageRecordStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = timestamp,
        Capability = AICapability.Chat,
        ProfileId = Guid.Empty,
        ProfileAlias = "profile",
        ProviderId = "openai",
        ModelId = "gpt",
        InputTokens = 1,
        OutputTokens = 1,
        TotalTokens = 2,
        DurationMs = 10,
        Status = status,
        CreatedAt = timestamp,
    };
}
