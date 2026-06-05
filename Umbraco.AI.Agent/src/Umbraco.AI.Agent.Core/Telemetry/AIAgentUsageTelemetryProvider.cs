using Microsoft.Extensions.Options;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Telemetry;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

namespace Umbraco.AI.Agent.Core.Telemetry;

/// <summary>
/// Contributes anonymous, aggregate Umbraco.AI.Agent usage information to the CMS telemetry report.
/// </summary>
/// <remarks>
/// Data is only ever sent when the site's telemetry level is set to <c>Detailed</c>, and is
/// suppressed entirely when <c>Umbraco:AI:Telemetry:Enabled</c> is <c>false</c>. Only counts,
/// enum names, and code-authored surface IDs are reported — see
/// <see cref="AIAgentUsageTelemetryConstants"/> for the complete safelist.
/// </remarks>
public sealed class AIAgentUsageTelemetryProvider : IDetailedTelemetryProvider
{
    private readonly IOptionsMonitor<AIUsageTelemetryOptions> _telemetryOptions;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _analyticsOptions;
    private readonly IAIAgentService _agentService;
    private readonly AIAgentSurfaceCollection _surfaces;
    private readonly IAIUsageAnalyticsService _usageAnalyticsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentUsageTelemetryProvider"/> class.
    /// </summary>
    public AIAgentUsageTelemetryProvider(
        IOptionsMonitor<AIUsageTelemetryOptions> telemetryOptions,
        IOptionsMonitor<AIAnalyticsOptions> analyticsOptions,
        IAIAgentService agentService,
        AIAgentSurfaceCollection surfaces,
        IAIUsageAnalyticsService usageAnalyticsService)
    {
        _telemetryOptions = telemetryOptions;
        _analyticsOptions = analyticsOptions;
        _agentService = agentService;
        _surfaces = surfaces;
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
            AIAgent[] agents = _agentService
                .GetAgentsAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
                .ToArray();

            (var surfaces, var customSurfaceCount) = AIUsageTelemetryClassification.ClassifyInUse(
                agents.SelectMany(a => a.SurfaceIds),
                AIUsageTelemetryClassification.GetSystemIds(_surfaces, s => s.Id));

            result.Add(new UsageInformation(AIAgentUsageTelemetryConstants.AgentCount, agents.Length));
            result.Add(new UsageInformation(AIAgentUsageTelemetryConstants.AgentActiveCount, agents.Count(a => a.IsActive)));
            result.Add(new UsageInformation(AIAgentUsageTelemetryConstants.AgentWithProfileCount, agents.Count(a => a.ProfileId.HasValue)));
            result.Add(new UsageInformation(AIAgentUsageTelemetryConstants.AgentWithGuardrailCount, agents.Count(a => a.GuardrailIds.Count > 0)));
            result.Add(new UsageInformation(AIAgentUsageTelemetryConstants.AgentSurfaces, surfaces));
            result.Add(new UsageInformation(AIAgentUsageTelemetryConstants.AgentSurfaceCustomCount, customSurfaceCount));

            foreach (var typeGroup in agents.GroupBy(a => a.AgentType))
            {
                result.Add(new UsageInformation(
                    AIAgentUsageTelemetryConstants.AgentCountPrefix + typeGroup.Key,
                    typeGroup.Count()));
            }
        }
        catch
        {
            // Best-effort: skip entity counts if the agent store is unavailable
        }

        try
        {
            if (_analyticsOptions.CurrentValue.Enabled)
            {
                DateTime to = DateTime.UtcNow;

                AIUsageSummary summary = _usageAnalyticsService
                    .GetSummaryAsync(to.AddDays(-30), to, filter: new AIUsageFilter { FeatureType = "agent" })
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                result.Add(new UsageInformation(AIAgentUsageTelemetryConstants.AgentExecutions30d, summary.TotalRequests));
            }
        }
        catch
        {
            // Best-effort: skip execution counts if analytics is unavailable
        }

        return result;
    }
}
