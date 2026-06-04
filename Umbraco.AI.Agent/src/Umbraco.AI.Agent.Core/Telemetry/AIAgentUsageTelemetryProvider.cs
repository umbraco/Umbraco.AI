using Microsoft.Extensions.Options;
using Umbraco.AI.Agent.Core.Agents;
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
/// <see cref="AIAgentUsageTelemetryConstants"/> for the complete whitelist.
/// </remarks>
public sealed class AIAgentUsageTelemetryProvider : IDetailedTelemetryProvider
{
    private readonly IOptionsMonitor<AIUsageTelemetryOptions> _telemetryOptions;
    private readonly IAIAgentService _agentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentUsageTelemetryProvider"/> class.
    /// </summary>
    public AIAgentUsageTelemetryProvider(
        IOptionsMonitor<AIUsageTelemetryOptions> telemetryOptions,
        IAIAgentService agentService)
    {
        _telemetryOptions = telemetryOptions;
        _agentService = agentService;
    }

    /// <inheritdoc />
    public IEnumerable<UsageInformation> GetInformation()
    {
        if (!_telemetryOptions.CurrentValue.Enabled)
        {
            return [];
        }

        try
        {
            AIAgent[] agents = _agentService
                .GetAgentsAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
                .ToArray();

            var surfaces = agents
                .SelectMany(a => a.SurfaceIds)
                .Select(s => s.ToLowerInvariant())
                .ToHashSet();

            var result = new List<UsageInformation>
            {
                new(AIAgentUsageTelemetryConstants.AgentCount, agents.Length),
                new(AIAgentUsageTelemetryConstants.AgentActiveCount, agents.Count(a => a.IsActive)),
                new(AIAgentUsageTelemetryConstants.AgentWithProfileCount, agents.Count(a => a.ProfileId.HasValue)),
                new(AIAgentUsageTelemetryConstants.AgentWithGuardrailCount, agents.Count(a => a.GuardrailIds.Count > 0)),
                new(AIAgentUsageTelemetryConstants.AgentSurfaces, surfaces),
            };

            foreach (var typeGroup in agents.GroupBy(a => a.AgentType))
            {
                result.Add(new UsageInformation(
                    AIAgentUsageTelemetryConstants.AgentCountPrefix + typeGroup.Key,
                    typeGroup.Count()));
            }

            return result;
        }
        catch
        {
            // Telemetry is strictly best-effort; never throw into the CMS ReportSiteJob.
            return [];
        }
    }
}
