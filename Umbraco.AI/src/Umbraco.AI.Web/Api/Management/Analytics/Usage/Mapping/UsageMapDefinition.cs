using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Web.Api.Management.Analytics.Usage.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Analytics.Usage.Mapping;

/// <summary>
/// Map definitions for Analytics models.
/// </summary>
public class UsageMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AIUsageSummary, UsageSummaryResponseModel>((_, _) => new UsageSummaryResponseModel(), MapToSummaryResponse);
        mapper.Define<AIUsageTimeSeriesPoint, UsageTimeSeriesPointModel>((_, _) => new UsageTimeSeriesPointModel(), MapToTimeSeriesPoint);
        mapper.Define<AIUsageBreakdownItem, UsageBreakdownItemModel>((_, _) => new UsageBreakdownItemModel(), MapToBreakdownItem);

        // Overview and breakdown rows (domain -> response)
        mapper.Define<AIUsageCounts, UsageCountsModel>((_, _) => new UsageCountsModel(), MapToCounts);
        mapper.Define<AIUsageBucket, UsageOverviewBucketModel>((_, _) => new UsageOverviewBucketModel(), MapToBucket);
        mapper.Define<AIUsageHourlyBucket, UsageOverviewHourlyBucketModel>((_, _) => new UsageOverviewHourlyBucketModel(), MapToHourlyBucket);
        mapper.Define<AIUsageOverviewPeriod, UsageOverviewPeriodModel>((_, _) => new UsageOverviewPeriodModel(), MapToOverviewPeriod);
        mapper.Define<AIUsageOverview, UsageOverviewResponseModel>((_, _) => new UsageOverviewResponseModel(), MapToOverview);
        mapper.Define<AIUsageBreakdownRow, UsageBreakdownRowModel>((_, _) => new UsageBreakdownRowModel(), MapToBreakdownRow);
        mapper.Define<AIUsageBreakdownPage, UsageBreakdownResponseModel>((_, _) => new UsageBreakdownResponseModel(), MapToBreakdownPage);
    }

    // Umbraco.Code.MapAll
    private static void MapToSummaryResponse(AIUsageSummary source, UsageSummaryResponseModel target, MapperContext context)
    {
        target.TotalRequests = source.TotalRequests;
        target.InputTokens = source.InputTokens;
        target.CachedInputTokens = source.CachedInputTokens;
        target.OutputTokens = source.OutputTokens;
        target.TotalTokens = source.TotalTokens;
        target.SuccessCount = source.SuccessCount;
        target.FailureCount = source.FailureCount;
        target.SuccessRate = source.SuccessRate;
        target.AverageDurationMs = source.AverageDurationMs;
    }

    // Umbraco.Code.MapAll
    private static void MapToTimeSeriesPoint(AIUsageTimeSeriesPoint source, UsageTimeSeriesPointModel target, MapperContext context)
    {
        target.Timestamp = source.Timestamp;
        target.RequestCount = source.RequestCount;
        target.InputTokens = source.InputTokens;
        target.CachedInputTokens = source.CachedInputTokens;
        target.OutputTokens = source.OutputTokens;
        target.TotalTokens = source.TotalTokens;
        target.SuccessCount = source.SuccessCount;
        target.FailureCount = source.FailureCount;
    }

    // Umbraco.Code.MapAll
    private static void MapToBreakdownItem(AIUsageBreakdownItem source, UsageBreakdownItemModel target, MapperContext context)
    {
        target.Dimension = source.Dimension;
        target.DimensionName = source.DimensionName;
        target.RequestCount = source.RequestCount;
        target.TotalTokens = source.TotalTokens;
        target.CachedInputTokens = source.CachedInputTokens;
        target.Percentage = source.Percentage;
    }

    // Umbraco.Code.MapAll
    private static void MapToCounts(AIUsageCounts source, UsageCountsModel target, MapperContext context)
    {
        target.Requests = source.Requests;
        target.NestedRequests = source.NestedRequests;
        target.Succeeded = source.Succeeded;
        target.Failed = source.Failed;
        target.InputTokens = source.InputTokens;
        target.CachedInputTokens = source.CachedInputTokens;
        target.OutputTokens = source.OutputTokens;
        target.TotalTokens = source.TotalTokens;
        target.DurationMs = source.DurationMs;
    }

    // Umbraco.Code.MapAll
    private static void MapToBucket(AIUsageBucket source, UsageOverviewBucketModel target, MapperContext context)
    {
        target.Start = source.Start;
        target.Requests = source.Requests;
        target.NestedRequests = source.NestedRequests;
        target.Succeeded = source.Succeeded;
        target.Failed = source.Failed;
        target.InputTokens = source.InputTokens;
        target.CachedInputTokens = source.CachedInputTokens;
        target.OutputTokens = source.OutputTokens;
        target.TotalTokens = source.TotalTokens;
        target.DurationMs = source.DurationMs;
    }

    // Umbraco.Code.MapAll
    private static void MapToHourlyBucket(AIUsageHourlyBucket source, UsageOverviewHourlyBucketModel target, MapperContext context)
    {
        target.Start = source.Start;
        target.Requests = source.Requests;
        target.NestedRequests = source.NestedRequests;
        target.Succeeded = source.Succeeded;
        target.Failed = source.Failed;
        target.InputTokens = source.InputTokens;
        target.CachedInputTokens = source.CachedInputTokens;
        target.OutputTokens = source.OutputTokens;
        target.TotalTokens = source.TotalTokens;
        target.DurationMs = source.DurationMs;
    }

    // Umbraco.Code.MapAll
    private static void MapToOverviewPeriod(AIUsageOverviewPeriod source, UsageOverviewPeriodModel target, MapperContext context)
    {
        target.From = source.From;
        target.To = source.To;
        target.Totals = context.Map<UsageCountsModel>(source.Totals)!;
        target.Buckets = context.MapEnumerable<AIUsageBucket, UsageOverviewBucketModel>(source.Buckets);
        target.HourlyBuckets = context.MapEnumerable<AIUsageHourlyBucket, UsageOverviewHourlyBucketModel>(source.HourlyBuckets);
    }

    // Umbraco.Code.MapAll
    private static void MapToOverview(AIUsageOverview source, UsageOverviewResponseModel target, MapperContext context)
    {
        target.Current = context.Map<UsageOverviewPeriodModel>(source.Current)!;
        target.Previous = source.Previous is null ? null : context.Map<UsageOverviewPeriodModel>(source.Previous);
    }

    // Umbraco.Code.MapAll
    private static void MapToBreakdownRow(AIUsageBreakdownRow source, UsageBreakdownRowModel target, MapperContext context)
    {
        target.Key = source.Key;
        target.Name = source.Name;
        target.Current = context.Map<UsageCountsModel>(source.Current)!;
        target.Previous = source.Previous is null ? null : context.Map<UsageCountsModel>(source.Previous);
    }

    // Umbraco.Code.MapAll
    private static void MapToBreakdownPage(AIUsageBreakdownPage source, UsageBreakdownResponseModel target, MapperContext context)
    {
        target.Total = source.Total;
        target.Items = context.MapEnumerable<AIUsageBreakdownRow, UsageBreakdownRowModel>(source.Items);
        target.Totals = context.Map<UsageCountsModel>(source.Totals)!;
        target.PreviousTotals = source.PreviousTotals is null ? null : context.Map<UsageCountsModel>(source.PreviousTotals);
    }
}
