using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Contributes anonymous, aggregate Umbraco.AI usage information to the CMS telemetry report.
/// </summary>
/// <remarks>
/// <para>
/// Data is gathered by the CMS <c>ReportSiteJob</c> alongside all other
/// <see cref="IDetailedTelemetryProvider"/> implementations and is only ever sent when the
/// site's telemetry level is set to <c>Detailed</c>. Additionally, setting
/// <c>Umbraco:AI:Telemetry:Enabled</c> to <c>false</c> suppresses all Umbraco.AI data
/// regardless of the CMS telemetry level.
/// </para>
/// <para>
/// Only counts, booleans, and normalized identifiers are reported — see
/// <see cref="AIUsageTelemetryConstants"/> for the complete whitelist. Not to be confused with
/// <see cref="AITelemetry"/>, which configures OpenTelemetry tracing/metrics for the host
/// application's own observability infrastructure and never leaves the customer's environment.
/// </para>
/// </remarks>
public sealed class AIUsageTelemetryProvider : IDetailedTelemetryProvider
{
    private readonly IOptionsMonitor<AIUsageTelemetryOptions> _telemetryOptions;
    private readonly IOptionsMonitor<AIOptions> _aiOptions;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;
    private readonly IOptionsMonitor<AIAnalyticsOptions> _analyticsOptions;
    private readonly AIProviderCollection _providers;
    private readonly IAIConnectionService _connectionService;
    private readonly IAIProfileService _profileService;
    private readonly IAIContextService _contextService;
    private readonly IAIGuardrailService _guardrailService;
    private readonly IAIUsageAnalyticsService _usageAnalyticsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIUsageTelemetryProvider"/> class.
    /// </summary>
    public AIUsageTelemetryProvider(
        IOptionsMonitor<AIUsageTelemetryOptions> telemetryOptions,
        IOptionsMonitor<AIOptions> aiOptions,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions,
        IOptionsMonitor<AIAnalyticsOptions> analyticsOptions,
        AIProviderCollection providers,
        IAIConnectionService connectionService,
        IAIProfileService profileService,
        IAIContextService contextService,
        IAIGuardrailService guardrailService,
        IAIUsageAnalyticsService usageAnalyticsService)
    {
        _telemetryOptions = telemetryOptions;
        _aiOptions = aiOptions;
        _auditLogOptions = auditLogOptions;
        _analyticsOptions = analyticsOptions;
        _providers = providers;
        _connectionService = connectionService;
        _profileService = profileService;
        _contextService = contextService;
        _guardrailService = guardrailService;
        _usageAnalyticsService = usageAnalyticsService;
    }

    /// <inheritdoc />
    public IEnumerable<UsageInformation> GetInformation()
    {
        if (!_telemetryOptions.CurrentValue.Enabled)
        {
            return [];
        }

        // Each section is gathered independently so a failure in one (e.g., persistence not
        // yet migrated) never prevents the rest from reporting — and never throws into the
        // CMS ReportSiteJob.
        var result = new List<UsageInformation>();

        TryCollect(result, CollectProviders);
        TryCollect(result, CollectConnections);
        TryCollect(result, CollectProfiles);
        TryCollect(result, CollectContextsAndGuardrails);
        TryCollect(result, CollectConfiguration);
        TryCollect(result, CollectUsage);

        return result;
    }

    private static void TryCollect(List<UsageInformation> result, Action<List<UsageInformation>> collect)
    {
        try
        {
            collect(result);
        }
        catch
        {
            // Telemetry is strictly best-effort; partial data is preferable to no data.
        }
    }

    private void CollectProviders(List<UsageInformation> result)
    {
        var providerIds = _providers
            .Select(p => p.Id.ToLowerInvariant())
            .ToHashSet();

        result.Add(new UsageInformation(AIUsageTelemetryConstants.Providers, providerIds));
    }

    private void CollectConnections(List<UsageInformation> result)
    {
        AIConnection[] connections = _connectionService
            .GetConnectionsAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult()
            .ToArray();

        var connectedProviders = connections
            .Select(c => c.ProviderId.ToLowerInvariant())
            .ToHashSet();

        result.Add(new UsageInformation(AIUsageTelemetryConstants.ConnectionCount, connections.Length));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.ConnectedProviders, connectedProviders));
    }

    private void CollectProfiles(List<UsageInformation> result)
    {
        AIProfile[] profiles = _profileService
            .GetAllProfilesAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult()
            .ToArray();

        result.Add(new UsageInformation(AIUsageTelemetryConstants.ProfileCount, profiles.Length));

        foreach (var capabilityGroup in profiles.GroupBy(p => p.Capability))
        {
            result.Add(new UsageInformation(
                AIUsageTelemetryConstants.ProfileCountPrefix + capabilityGroup.Key,
                capabilityGroup.Count()));
        }
    }

    private void CollectContextsAndGuardrails(List<UsageInformation> result)
    {
        (_, var contextCount) = _contextService
            .GetContextsPagedAsync(take: 0)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        (_, var guardrailCount) = _guardrailService
            .GetGuardrailsPagedAsync(take: 0)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        result.Add(new UsageInformation(AIUsageTelemetryConstants.ContextCount, contextCount));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.GuardrailCount, guardrailCount));
    }

    private void CollectConfiguration(List<UsageInformation> result)
    {
        AIOptions aiOptions = _aiOptions.CurrentValue;

        var defaultProfileCapabilities = new HashSet<string>();

        if (!string.IsNullOrWhiteSpace(aiOptions.DefaultChatProfileAlias))
        {
            defaultProfileCapabilities.Add(nameof(AICapability.Chat));
        }

        if (!string.IsNullOrWhiteSpace(aiOptions.DefaultEmbeddingProfileAlias))
        {
            defaultProfileCapabilities.Add(nameof(AICapability.Embedding));
        }

        if (!string.IsNullOrWhiteSpace(aiOptions.DefaultSpeechToTextProfileAlias))
        {
            defaultProfileCapabilities.Add(nameof(AICapability.SpeechToText));
        }

        result.Add(new UsageInformation(AIUsageTelemetryConstants.DefaultProfileCapabilities, defaultProfileCapabilities));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.AuditLogEnabled, _auditLogOptions.CurrentValue.Enabled));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.AnalyticsEnabled, _analyticsOptions.CurrentValue.Enabled));
    }

    private void CollectUsage(List<UsageInformation> result)
    {
        if (!_analyticsOptions.CurrentValue.Enabled)
        {
            return;
        }

        DateTime to = DateTime.UtcNow;
        DateTime from = to.AddDays(-30);

        AIUsageSummary summary = _usageAnalyticsService
            .GetSummaryAsync(from, to)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        // Request counts and success rate only — token totals are deliberately excluded
        // as they are a proxy for customer spend.
        result.Add(new UsageInformation(AIUsageTelemetryConstants.UsageRequests30d, summary.TotalRequests));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.UsageSuccessRate30d, Math.Round(summary.SuccessRate, 4)));
    }
}
