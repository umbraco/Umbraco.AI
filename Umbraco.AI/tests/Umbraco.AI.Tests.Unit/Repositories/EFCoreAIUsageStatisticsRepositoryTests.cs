using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Persistence;
using Umbraco.AI.Persistence.Analytics.Usage;
using Umbraco.AI.Tests.Common.Fixtures;

namespace Umbraco.AI.Tests.Unit.Repositories;

/// <summary>
/// The usage statistics are summed in the database (GROUP BY and SUM), so a dashboard never reads every
/// combination of dimensions. Runs on SQLite; the query uses no provider-specific SQL.
/// </summary>
public class EFCoreAIUsageStatisticsRepositoryTests : IClassFixture<EFCoreTestFixture>
{
    private static readonly DateTime Hour = new(2026, 1, 14, 10, 0, 0, DateTimeKind.Utc);

    private readonly EFCoreTestFixture _fixture;

    public EFCoreAIUsageStatisticsRepositoryTests(EFCoreTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetHourlyTotalsAsync_GroupedByPeriod_SumsEveryDimensionCombinationOfAnHour()
    {
        // Arrange: three rows in one hour (different model, user, capability), one in the next.
        var at = Unique(Hour);
        await SeedHourlyAsync(
            Row(at, requests: 3, failures: 1, nested: 1, cached: 5, model: "a", userId: "u1"),
            Row(at, requests: 2, model: "b", userId: "u2", capability: 1),
            Row(at, requests: 1, model: "a", userId: null),
            Row(at.AddHours(1), requests: 10));

        // Act
        var totals = await CreateRepository().GetHourlyTotalsAsync(at, at.AddHours(2), dimension: null);

        // Assert
        totals.Count.ShouldBe(2);
        var first = totals.Single(t => t.Period == at);
        first.RequestCount.ShouldBe(6);
        first.NestedRequestCount.ShouldBe(1);
        first.SuccessCount.ShouldBe(5);
        first.FailureCount.ShouldBe(1);
        first.InputTokens.ShouldBe(6);
        first.OutputTokens.ShouldBe(6);
        first.TotalTokens.ShouldBe(12);
        first.TotalDurationMs.ShouldBe(60);
        first.CachedInputTokens.ShouldBe(5);
        first.Key.ShouldBeNull();
        totals.Single(t => t.Period == at.AddHours(1)).RequestCount.ShouldBe(10);
    }

    [Fact]
    public async Task GetHourlyTotalsAsync_WhenNoRowReportsCachedTokens_LeavesThemNull()
    {
        // Arrange
        var at = Unique(Hour);
        await SeedHourlyAsync(Row(at, requests: 1), Row(at, requests: 1, model: "b"));

        // Act
        var totals = await CreateRepository().GetHourlyTotalsAsync(at, at.AddHours(1), dimension: null);

        // Assert
        totals.Single().CachedInputTokens.ShouldBeNull();
    }

    [Fact]
    public async Task GetHourlyTotalsAsync_ReadsOnlyTheHoursInTheWindow()
    {
        // Arrange
        var at = Unique(Hour);
        await SeedHourlyAsync(Row(at.AddHours(-1), requests: 1), Row(at, requests: 10), Row(at.AddHours(1), requests: 100));

        // Act
        var totals = await CreateRepository().GetHourlyTotalsAsync(at, at.AddHours(1), dimension: null);

        // Assert: the window is start inclusive, end exclusive.
        totals.Single().RequestCount.ShouldBe(10);
    }

    [Fact]
    public async Task GetHourlyTotalsAsync_GroupedByModel_OneRowPerModelAcrossHours()
    {
        // Arrange
        var at = Unique(Hour);
        await SeedHourlyAsync(
            Row(at, requests: 1, model: "gpt"), Row(at.AddHours(1), requests: 2, model: "gpt"),
            Row(at, requests: 4, model: "claude", userId: "u1"));

        // Act
        var totals = await CreateRepository().GetHourlyTotalsAsync(at, at.AddHours(2), AIUsageBreakdownDimension.Model);

        // Assert
        totals.Select(t => (t.Key, t.RequestCount)).OrderBy(t => t.Key).ShouldBe([("claude", 4), ("gpt", 3)]);
        totals.Single(t => t.Key == "gpt").Period.ShouldBe(at.AddHours(1));
    }

    [Fact]
    public async Task GetHourlyTotalsAsync_GroupedByProvider_OneRowPerProvider()
    {
        // Arrange
        var at = Unique(Hour);
        await SeedHourlyAsync(Row(at, requests: 1, provider: "openai"), Row(at, requests: 2, provider: "anthropic"), Row(at, requests: 3, provider: "openai", model: "x"));

        // Act
        var totals = await CreateRepository().GetHourlyTotalsAsync(at, at.AddHours(1), AIUsageBreakdownDimension.Provider);

        // Assert
        totals.Select(t => (t.Key, t.RequestCount)).OrderBy(t => t.Key).ShouldBe([("anthropic", 2), ("openai", 4)]);
    }

    [Fact]
    public async Task GetHourlyTotalsAsync_GroupedByProfile_KeysByIdAndKeepsEachAliasSeparate()
    {
        // Arrange: one profile renamed between hours, another profile.
        var at = Unique(Hour);
        var renamed = Guid.NewGuid();
        var other = Guid.NewGuid();
        await SeedHourlyAsync(
            Row(at, requests: 1, profileId: renamed, profileAlias: "old"),
            Row(at.AddHours(1), requests: 2, profileId: renamed, profileAlias: "new"),
            Row(at, requests: 4, profileId: other, profileAlias: "other"));

        // Act
        var totals = await CreateRepository().GetHourlyTotalsAsync(at, at.AddHours(2), AIUsageBreakdownDimension.Profile);

        // Assert: the service picks the name of the latest period per key.
        totals.Where(t => t.Key == renamed.ToString()).Select(t => (t.Name, t.Period, t.RequestCount)).OrderBy(t => t.Period)
            .ShouldBe([("old", at, 1), ("new", at.AddHours(1), 2)]);
        totals.Single(t => t.Key == other.ToString()).RequestCount.ShouldBe(4);
    }

    [Fact]
    public async Task GetHourlyTotalsAsync_GroupedByUser_KeepsUsersWithoutAnIdTogether()
    {
        // Arrange
        var at = Unique(Hour);
        await SeedHourlyAsync(
            Row(at, requests: 1, userId: null), Row(at, requests: 2, userId: null, model: "b"),
            Row(at, requests: 4, userId: "u1", userName: "Ada"));

        // Act
        var totals = await CreateRepository().GetHourlyTotalsAsync(at, at.AddHours(1), AIUsageBreakdownDimension.User);

        // Assert
        totals.Single(t => t.Key == null).RequestCount.ShouldBe(3);
        totals.Single(t => t.Key == "u1").Name.ShouldBe("Ada");
    }

    [Fact]
    public async Task GetDailyTotalsAsync_GroupedByPeriodAndByDimension_SumsTheDailyTable()
    {
        // Arrange
        var day = Unique(Hour.Date);
        await SeedDailyAsync(
            Row(day, requests: 3, model: "gpt"), Row(day, requests: 2, model: "claude"),
            Row(day.AddDays(1), requests: 10, model: "gpt"));

        // Act
        var repository = CreateRepository();
        var byDay = await repository.GetDailyTotalsAsync(day, day.AddDays(2), dimension: null);
        var byModel = await repository.GetDailyTotalsAsync(day, day.AddDays(2), AIUsageBreakdownDimension.Model);

        // Assert
        byDay.Select(t => (t.Period, t.RequestCount)).OrderBy(t => t.Period).ShouldBe([(day, 5), (day.AddDays(1), 10)]);
        byModel.Select(t => (t.Key, t.RequestCount)).OrderBy(t => t.Key).ShouldBe([("claude", 2), ("gpt", 13)]);
    }

    private EFCoreAIUsageStatisticsRepository CreateRepository()
        => new(new TestEFCoreScopeProvider(() => _fixture.CreateContext()), NullLogger<EFCoreAIUsageStatisticsRepository>.Instance);

    private async Task SeedHourlyAsync(params AIUsageStatisticsRow[] rows)
    {
        await using var context = _fixture.CreateContext();
        context.UsageStatisticsHourly.AddRange(rows.Select(r => new AIUsageStatisticsHourlyEntity
        {
            Id = Guid.NewGuid(),
            Period = r.Period,
            ProviderId = r.Provider,
            ModelId = r.Model,
            ProfileId = r.ProfileId,
            Capability = r.Capability,
            UserId = r.UserId,
            UserName = r.UserName,
            ProfileAlias = r.ProfileAlias,
            RequestCount = r.Requests,
            NestedRequestCount = r.Nested,
            SuccessCount = r.Requests - r.Failures,
            FailureCount = r.Failures,
            InputTokens = r.Requests,
            CachedInputTokens = r.Cached,
            OutputTokens = r.Requests,
            TotalTokens = r.Requests * 2,
            TotalDurationMs = r.Requests * 10,
            CreatedAt = r.Period,
        }));
        await context.SaveChangesAsync();
    }

    private async Task SeedDailyAsync(params AIUsageStatisticsRow[] rows)
    {
        await using var context = _fixture.CreateContext();
        context.UsageStatisticsDaily.AddRange(rows.Select(r => new AIUsageStatisticsDailyEntity
        {
            Id = Guid.NewGuid(),
            Period = r.Period,
            ProviderId = r.Provider,
            ModelId = r.Model,
            ProfileId = r.ProfileId,
            Capability = r.Capability,
            UserId = r.UserId,
            UserName = r.UserName,
            ProfileAlias = r.ProfileAlias,
            RequestCount = r.Requests,
            NestedRequestCount = r.Nested,
            SuccessCount = r.Requests - r.Failures,
            FailureCount = r.Failures,
            InputTokens = r.Requests,
            CachedInputTokens = r.Cached,
            OutputTokens = r.Requests,
            TotalTokens = r.Requests * 2,
            TotalDurationMs = r.Requests * 10,
            CreatedAt = r.Period,
        }));
        await context.SaveChangesAsync();
    }

    // The fixture's database is shared by every test in the class, so each test works in a period of its own.
    private static int _offset;

    private static DateTime Unique(DateTime baseTime) => baseTime.AddYears(-Interlocked.Increment(ref _offset));

    private static AIUsageStatisticsRow Row(
        DateTime period,
        int requests,
        int failures = 0,
        int nested = 0,
        long? cached = null,
        string provider = "openai",
        string model = "gpt",
        Guid? profileId = null,
        string? profileAlias = "profile",
        string? userId = "u1",
        string? userName = null,
        int capability = 0)
        => new(period, requests, failures, nested, cached, provider, model, profileId ?? Guid.Empty, profileAlias, userId, userName, capability);

    private sealed record AIUsageStatisticsRow(
        DateTime Period,
        int Requests,
        int Failures,
        int Nested,
        long? Cached,
        string Provider,
        string Model,
        Guid ProfileId,
        string? ProfileAlias,
        string? UserId,
        string? UserName,
        int Capability);
}
