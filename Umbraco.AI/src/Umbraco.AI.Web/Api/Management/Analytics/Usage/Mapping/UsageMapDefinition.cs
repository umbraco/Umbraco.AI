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
    }

    // Umbraco.Code.MapAll
    private static void MapToSummaryResponse(AIUsageSummary source, UsageSummaryResponseModel target, MapperContext context)
    {
        target.TotalRequests = source.TotalRequests;
        target.InputTokens = source.InputTokens;
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
        target.Percentage = source.Percentage;
    }
}
