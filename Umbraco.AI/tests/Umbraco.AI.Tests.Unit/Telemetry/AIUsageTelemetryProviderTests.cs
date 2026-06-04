using System.Text.Json;
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
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Tests.Unit.Telemetry;

public class AIUsageTelemetryProviderTests
{
    // Sensitive fixture values that must never appear anywhere in emitted telemetry
    private const string SensitiveAlias = "acme-secret-project";
    private const string SensitiveModelId = "acme-merger-deployment";
    private const string SensitiveApiKey = "sk-acme-super-secret-key";

    private readonly Mock<IAIConnectionService> _connectionService = new();
    private readonly Mock<IAIProfileService> _profileService = new();
    private readonly Mock<IAIContextService> _contextService = new();
    private readonly Mock<IAIGuardrailService> _guardrailService = new();
    private readonly Mock<IAIUsageAnalyticsService> _usageAnalyticsService = new();

    private AIUsageTelemetryOptions _telemetryOptions = new();
    private AIOptions _aiOptions = new();
    private AIAuditLogOptions _auditLogOptions = new();
    private AIAnalyticsOptions _analyticsOptions = new();

    public AIUsageTelemetryProviderTests()
    {
        _connectionService.Setup(x => x.GetConnectionsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AIConnectionBuilder()
                    .WithAlias(SensitiveAlias)
                    .WithProviderId("openai")
                    .WithSettings(new { ApiKey = SensitiveApiKey })
                    .Build(),
            ]);

        _profileService.Setup(x => x.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AIProfileBuilder().WithAlias(SensitiveAlias).WithCapability(AICapability.Chat).WithModel("openai", "gpt-4o").Build(),
                new AIProfileBuilder().WithCapability(AICapability.Chat).WithModel("openai", SensitiveModelId).Build(),
                new AIProfileBuilder().WithCapability(AICapability.Embedding).WithModel("openai", "text-embedding-3-small").Build(),
            ]);

        _contextService.Setup(x => x.GetContextsPagedAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 5));

        _guardrailService.Setup(x => x.GetGuardrailsPagedAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 2));

        _usageAnalyticsService.Setup(x => x.GetSummaryAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<AIUsagePeriod?>(), It.IsAny<AIUsageFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIUsageSummary
            {
                TotalRequests = 100,
                InputTokens = 1000,
                OutputTokens = 2000,
                TotalTokens = 3000,
                SuccessCount = 95,
                FailureCount = 5,
                SuccessRate = 0.95,
                AverageDurationMs = 1234,
            });
    }

    private AIUsageTelemetryProvider CreateProvider(params IAIProvider[] providers)
    {
        IAIProvider[] effectiveProviders = providers.Length > 0
            ? providers
            : [new FakeAIProvider("openai"), new FakeAIProvider("anthropic")];

        return new AIUsageTelemetryProvider(
            MonitorOf(_telemetryOptions),
            MonitorOf(_aiOptions),
            MonitorOf(_auditLogOptions),
            MonitorOf(_analyticsOptions),
            new AIProviderCollection(() => effectiveProviders),
            _connectionService.Object,
            _profileService.Object,
            _contextService.Object,
            _guardrailService.Object,
            _usageAnalyticsService.Object);
    }

    private static IOptionsMonitor<T> MonitorOf<T>(T value)
    {
        var monitor = new Mock<IOptionsMonitor<T>>();
        monitor.Setup(x => x.CurrentValue).Returns(value);
        return monitor.Object;
    }

    [Fact]
    public void GetInformation_WhenDisabled_ReturnsEmpty()
    {
        _telemetryOptions = new AIUsageTelemetryOptions { Enabled = false };

        var result = CreateProvider().GetInformation();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetInformation_EmitsOnlyWhitelistedKeys()
    {
        var whitelist = typeof(AIUsageTelemetryConstants)
            .GetFields()
            .Select(f => (string)f.GetValue(null)!)
            .ToHashSet();

        // Per-capability profile counts use the documented dynamic prefix
        var capabilityKeys = Enum.GetNames<AICapability>()
            .Select(name => AIUsageTelemetryConstants.ProfileCountPrefix + name);

        whitelist.UnionWith(capabilityKeys);

        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        result.ShouldNotBeEmpty();
        foreach (UsageInformation info in result)
        {
            whitelist.ShouldContain(info.Name);
        }
    }

    [Fact]
    public void GetInformation_DoesNotLeakSensitiveValues()
    {
        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        var payload = JsonSerializer.Serialize(result.Select(i => new { i.Name, i.Data }));

        payload.ShouldNotContain(SensitiveAlias, Case.Insensitive);
        payload.ShouldNotContain(SensitiveModelId, Case.Insensitive);
        payload.ShouldNotContain(SensitiveApiKey, Case.Insensitive);
        payload.ShouldNotContain("acme", Case.Insensitive);
    }

    [Fact]
    public void GetInformation_ReportsExpectedCounts()
    {
        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        GetData(result, AIUsageTelemetryConstants.ProfileCount).ShouldBe(3);
        GetData(result, AIUsageTelemetryConstants.ProfileCountPrefix + nameof(AICapability.Chat)).ShouldBe(2);
        GetData(result, AIUsageTelemetryConstants.ProfileCountPrefix + nameof(AICapability.Embedding)).ShouldBe(1);
        GetData(result, AIUsageTelemetryConstants.ConnectionCount).ShouldBe(1);
        GetData(result, AIUsageTelemetryConstants.ContextCount).ShouldBe(5);
        GetData(result, AIUsageTelemetryConstants.GuardrailCount).ShouldBe(2);
        GetData(result, AIUsageTelemetryConstants.UsageRequests30d).ShouldBe(100);
        GetData(result, AIUsageTelemetryConstants.UsageSuccessRate30d).ShouldBe(0.95);

        var providers = GetData(result, AIUsageTelemetryConstants.Providers).ShouldBeAssignableTo<IEnumerable<string>>();
        providers.ShouldContain("openai");
        providers.ShouldContain("anthropic");
    }

    [Fact]
    public void GetInformation_ExcludesTokenTotals()
    {
        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        // Token counts are a proxy for customer spend and must never be reported
        result.ShouldNotContain(i => i.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetInformation_WhenAnalyticsDisabled_OmitsUsageMetrics()
    {
        _analyticsOptions = new AIAnalyticsOptions { Enabled = false };

        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        result.ShouldNotContain(i => i.Name == AIUsageTelemetryConstants.UsageRequests30d);
        result.ShouldNotContain(i => i.Name == AIUsageTelemetryConstants.UsageSuccessRate30d);
    }

    [Fact]
    public void GetInformation_WhenOneSectionThrows_StillReportsOtherSections()
    {
        _connectionService.Setup(x => x.GetConnectionsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Persistence not available"));

        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        result.ShouldNotContain(i => i.Name == AIUsageTelemetryConstants.ConnectionCount);
        result.ShouldContain(i => i.Name == AIUsageTelemetryConstants.ProfileCount);
        result.ShouldContain(i => i.Name == AIUsageTelemetryConstants.Providers);
    }

    private static object GetData(UsageInformation[] result, string name)
        => result.Single(i => i.Name == name).Data;
}
