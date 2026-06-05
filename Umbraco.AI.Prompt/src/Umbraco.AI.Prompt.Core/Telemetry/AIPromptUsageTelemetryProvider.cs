using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

namespace Umbraco.AI.Prompt.Core.Telemetry;

/// <summary>
/// Contributes anonymous, aggregate Umbraco.AI.Prompt usage information to the CMS telemetry report.
/// </summary>
/// <remarks>
/// Data is only ever sent when the site's telemetry level is set to <c>Detailed</c>, and is
/// suppressed entirely when <c>Umbraco:AI:Telemetry:Enabled</c> is <c>false</c>. Only counts
/// and enum names are reported — see <see cref="AIPromptUsageTelemetryConstants"/> for the
/// complete safelist.
/// </remarks>
public sealed class AIPromptUsageTelemetryProvider : IDetailedTelemetryProvider
{
    private readonly IOptionsMonitor<AIUsageTelemetryOptions> _telemetryOptions;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _analyticsOptions;
    private readonly IAIPromptService _promptService;
    private readonly IAIUsageAnalyticsService _usageAnalyticsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptUsageTelemetryProvider"/> class.
    /// </summary>
    public AIPromptUsageTelemetryProvider(
        IOptionsMonitor<AIUsageTelemetryOptions> telemetryOptions,
        IOptionsMonitor<AIAnalyticsOptions> analyticsOptions,
        IAIPromptService promptService,
        IAIUsageAnalyticsService usageAnalyticsService)
    {
        _telemetryOptions = telemetryOptions;
        _analyticsOptions = analyticsOptions;
        _promptService = promptService;
        _usageAnalyticsService = usageAnalyticsService;
    }

    /// <inheritdoc />
    public IEnumerable<UsageInformation> GetInformation()
    {
        if (!_telemetryOptions.CurrentValue.Enabled)
        {
            return [];
        }

        // Sections are gathered independently so a failure in one never prevents the rest
        // from reporting - and never throws into the CMS ReportSiteJob.
        var result = new List<UsageInformation>();

        try
        {
            AIPrompt[] prompts = _promptService
                .GetPromptsAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
                .ToArray();

            var displayModes = prompts
                .Select(p => p.DisplayMode.ToString())
                .ToHashSet();

            result.Add(new UsageInformation(AIPromptUsageTelemetryConstants.PromptCount, prompts.Length));
            result.Add(new UsageInformation(AIPromptUsageTelemetryConstants.PromptActiveCount, prompts.Count(p => p.IsActive)));
            result.Add(new UsageInformation(AIPromptUsageTelemetryConstants.PromptWithProfileCount, prompts.Count(p => p.ProfileId.HasValue)));
            result.Add(new UsageInformation(AIPromptUsageTelemetryConstants.PromptWithContextCount, prompts.Count(p => p.ContextIds.Count > 0)));
            result.Add(new UsageInformation(AIPromptUsageTelemetryConstants.PromptWithGuardrailCount, prompts.Count(p => p.GuardrailIds.Count > 0)));
            result.Add(new UsageInformation(AIPromptUsageTelemetryConstants.PromptDisplayModes, displayModes));
        }
        catch
        {
            // Best-effort: skip entity counts if the prompt store is unavailable
        }

        try
        {
            if (_analyticsOptions.CurrentValue.Enabled)
            {
                DateTime to = DateTime.UtcNow;

                AIUsageSummary summary = _usageAnalyticsService
                    .GetSummaryAsync(to.AddDays(-30), to, filter: new AIUsageFilter { FeatureType = "prompt" })
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                result.Add(new UsageInformation(AIPromptUsageTelemetryConstants.PromptExecutions30d, summary.TotalRequests));
            }
        }
        catch
        {
            // Best-effort: skip execution counts if analytics is unavailable
        }

        return result;
    }
}
