using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using System.Reflection;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Tests;
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
    private readonly AIGuardrailEvaluatorCollection _guardrailEvaluators;
    private readonly IAITestService _testService;
    private readonly IAITestRunService _testRunService;
    private readonly AITestFeatureCollection _testFeatures;
    private readonly AITestGraderCollection _testGraders;
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
        AIGuardrailEvaluatorCollection guardrailEvaluators,
        IAITestService testService,
        IAITestRunService testRunService,
        AITestFeatureCollection testFeatures,
        AITestGraderCollection testGraders,
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
        _guardrailEvaluators = guardrailEvaluators;
        _testService = testService;
        _testRunService = testRunService;
        _testFeatures = testFeatures;
        _testGraders = testGraders;
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
        TryCollect(result, CollectTests);
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

        // Full fetch (rather than a paged count) so evaluator IDs can be aggregated
        AIGuardrail[] guardrails = _guardrailService
            .GetGuardrailsAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult()
            .ToArray();

        (var evaluators, var customEvaluatorCount) = AIUsageTelemetryClassification.ClassifyInUse(
            guardrails.SelectMany(g => g.Rules).Select(r => r.EvaluatorId),
            AIUsageTelemetryClassification.GetSystemIds(_guardrailEvaluators, e => e.Id));

        result.Add(new UsageInformation(AIUsageTelemetryConstants.ContextCount, contextCount));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.GuardrailCount, guardrails.Length));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.GuardrailEvaluators, evaluators));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.GuardrailEvaluatorCustomCount, customEvaluatorCount));
    }

    private void CollectTests(List<UsageInformation> result)
    {
        AITest[] tests = _testService
            .GetTestsAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult()
            .ToArray();

        (_, var testRunCount) = _testRunService
            .GetRunsPagedAsync(take: 0)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        (var testFeatures, var customFeatureCount) = AIUsageTelemetryClassification.ClassifyInUse(
            tests.Select(t => t.TestFeatureId),
            AIUsageTelemetryClassification.GetSystemIds(_testFeatures, f => f.Id));

        (var testGraders, var customGraderCount) = AIUsageTelemetryClassification.ClassifyInUse(
            tests.SelectMany(t => t.Graders).Select(g => g.GraderTypeId),
            AIUsageTelemetryClassification.GetSystemIds(_testGraders, g => g.Id));

        result.Add(new UsageInformation(AIUsageTelemetryConstants.TestCount, tests.Length));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.TestRunCount, testRunCount));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.TestFeatures, testFeatures));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.TestFeatureCustomCount, customFeatureCount));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.TestGraders, testGraders));
        result.Add(new UsageInformation(AIUsageTelemetryConstants.TestGraderCustomCount, customGraderCount));
    }

    private void CollectConfiguration(List<UsageInformation> result)
    {
        AIOptions aiOptions = _aiOptions.CurrentValue;

        // Capabilities with a configured default are discovered from the Default{Capability}ProfileAlias
        // properties on AIOptions, so new capabilities are reported without changes here.
        var defaultProfileCapabilities = typeof(AIOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string)
                && p.Name.StartsWith("Default", StringComparison.Ordinal)
                && p.Name.EndsWith("ProfileAlias", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace((string?)p.GetValue(aiOptions)))
            .Select(p => p.Name["Default".Length..^"ProfileAlias".Length])
            .ToHashSet();

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
