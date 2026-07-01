using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Analytics;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

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
    private readonly Mock<IAITestService> _testService = new();
    private readonly Mock<IAITestRunService> _testRunService = new();
    private readonly Mock<IAIUsageAnalyticsService> _usageAnalyticsService = new();
    private readonly Mock<IDataTypeService> _dataTypeService = new();
    private readonly Mock<IDataTypeUsageService> _dataTypeUsageService = new();

    private AIUsageTelemetryOptions _telemetryOptions = new();
    private AIOptions _aiOptions = new();
    private AIAuditLogOptions _auditLogOptions = new();
    private AIAnalyticsOptions _analyticsOptions = new();
    private AIExperimentalOptions _experimentalOptions = new();

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

        _guardrailService.Setup(x => x.GetGuardrailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AIGuardrailBuilder()
                    .WithAlias(SensitiveAlias)
                    .WithName(SensitiveAlias)
                    .WithRules(
                        new AIGuardrailRuleBuilder().WithEvaluatorId("regex").WithName(SensitiveAlias).Build(),
                        new AIGuardrailRuleBuilder().WithEvaluatorId("pii").Build(),
                        // Custom evaluator (not in the system registry) - must be counted, never named
                        new AIGuardrailRuleBuilder().WithEvaluatorId("acme-compliance-check").Build())
                    .Build(),
                new AIGuardrailBuilder().Build(),
            ]);

        _testService.Setup(x => x.GetTestsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AITest
                {
                    Alias = SensitiveAlias,
                    Name = SensitiveAlias,
                    TestFeatureId = "prompt",
                    TestTargetId = Guid.NewGuid(),
                    Graders =
                    [
                        new AITestGraderConfig
                        {
                            GraderTypeId = "contains",
                            Name = SensitiveAlias,
                        },
                        // Custom grader (not in the system registry) - must be counted, never named
                        new AITestGraderConfig
                        {
                            GraderTypeId = "acme-scorer",
                            Name = SensitiveAlias,
                        },
                    ],
                },
                new AITest
                {
                    Alias = "second-test",
                    Name = "Second Test",
                    // Custom test feature (not in the system registry) - must be counted, never named
                    TestFeatureId = "acme-workflow",
                    TestTargetId = Guid.NewGuid(),
                },
            ]);

        _testRunService.Setup(x => x.GetRunsPagedAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<AITestRunStatus?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 42));

        var contextPickerDataTypeKey = Guid.NewGuid();
        _dataTypeService.Setup(x => x.GetByEditorAliasAsync(Core.Constants.PropertyEditors.Aliases.ContextPicker))
            .ReturnsAsync([Mock.Of<IDataType>(d => d.Key == contextPickerDataTypeKey)]);

        // Two content types reference the picker data type, and content values exist
        _dataTypeService.Setup(x => x.GetPagedRelationsAsync(contextPickerDataTypeKey, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedModel<RelationItemModel> { Total = 2, Items = [] });

        _dataTypeUsageService.Setup(x => x.HasSavedValuesAsync(contextPickerDataTypeKey))
            .ReturnsAsync(Attempt.SucceedWithStatus(DataTypeOperationStatus.Success, true));

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
        // Custom provider: a Moq proxy lives in DynamicProxyGenAssembly2 -> classified custom
        var customProvider = new Mock<IAIProvider>();
        customProvider.Setup(x => x.Id).Returns("acme-internal-llm");

        IAIProvider[] effectiveProviders = providers.Length > 0
            ? providers
            : [new FakeAIProvider("openai"), new FakeAIProvider("anthropic"), customProvider.Object];

        return new AIUsageTelemetryProvider(
            MonitorOf(_telemetryOptions),
            MonitorOf(_aiOptions),
            MonitorOf(_auditLogOptions),
            MonitorOf(_analyticsOptions),
            MonitorOf(_experimentalOptions),
            new AIProviderCollection(() => effectiveProviders),
            _connectionService.Object,
            _profileService.Object,
            _contextService.Object,
            _guardrailService.Object,
            new AIGuardrailEvaluatorCollection(() => [new FakeGuardrailEvaluator("regex"), new FakeGuardrailEvaluator("pii")]),
            _testService.Object,
            _testRunService.Object,
            new AITestFeatureCollection(() => [new FakeTestFeature("prompt")]),
            new AITestGraderCollection(() => [new FakeTestGrader("contains")]),
            _usageAnalyticsService.Object,
            _dataTypeService.Object,
            _dataTypeUsageService.Object);
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
    public void GetInformation_EmitsOnlySafelistedKeys()
    {
        var safelist = typeof(AIUsageTelemetryConstants)
            .GetFields()
            .Select(f => (string)f.GetValue(null)!)
            .ToHashSet();

        // Per-capability profile and usage counts use the documented dynamic prefixes
        foreach (var name in Enum.GetNames<AICapability>())
        {
            safelist.Add(AIUsageTelemetryConstants.ProfileCountPrefix + name);
            safelist.Add(AIUsageTelemetryConstants.UsageRequests30dPrefix + name);
        }

        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        result.ShouldNotBeEmpty();
        foreach (UsageInformation info in result)
        {
            safelist.ShouldContain(info.Name);
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
        GetData(result, AIUsageTelemetryConstants.ContextPickerDataTypeCount).ShouldBe(1);
        GetData(result, AIUsageTelemetryConstants.ContextPickerContentTypeCount).ShouldBe(2);
        GetData(result, AIUsageTelemetryConstants.ContextPickerHasSavedValues).ShouldBe(true);
        GetData(result, AIUsageTelemetryConstants.GuardrailCount).ShouldBe(2);
        GetData(result, AIUsageTelemetryConstants.TestCount).ShouldBe(2);
        GetData(result, AIUsageTelemetryConstants.TestRunCount).ShouldBe(42);

        // System-registered IDs are reported verbatim; custom IDs only as distinct counts
        var testFeatures = GetData(result, AIUsageTelemetryConstants.TestFeatures).ShouldBeAssignableTo<IEnumerable<string>>()!.ToArray();
        testFeatures.ShouldBe(["prompt"]);
        GetData(result, AIUsageTelemetryConstants.TestFeatureCustomCount).ShouldBe(1);

        var testGraders = GetData(result, AIUsageTelemetryConstants.TestGraders).ShouldBeAssignableTo<IEnumerable<string>>()!.ToArray();
        testGraders.ShouldBe(["contains"]);
        GetData(result, AIUsageTelemetryConstants.TestGraderCustomCount).ShouldBe(1);

        var evaluators = GetData(result, AIUsageTelemetryConstants.GuardrailEvaluators).ShouldBeAssignableTo<IEnumerable<string>>()!.ToArray();
        evaluators.ShouldBe(["regex", "pii"], ignoreOrder: true);
        GetData(result, AIUsageTelemetryConstants.GuardrailEvaluatorCustomCount).ShouldBe(1);
        GetData(result, AIUsageTelemetryConstants.UsageRequests30d).ShouldBe(100);
        GetData(result, AIUsageTelemetryConstants.UsageSuccessRate30d).ShouldBe(0.95);

        // System providers named; the custom Moq provider only counted
        var providers = GetData(result, AIUsageTelemetryConstants.Providers).ShouldBeAssignableTo<IEnumerable<string>>()!.ToArray();
        providers.ShouldBe(["openai", "anthropic"], ignoreOrder: true);
        GetData(result, AIUsageTelemetryConstants.ProviderCustomCount).ShouldBe(1);
        GetData(result, AIUsageTelemetryConstants.ConnectedProviderCustomCount).ShouldBe(0);

        // Per-capability usage uses the dynamic prefix (mock returns the same summary per filter)
        GetData(result, AIUsageTelemetryConstants.UsageRequests30dPrefix + nameof(AICapability.Chat)).ShouldBe(100);
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
    public void GetInformation_WhenExperimentalFeatureEnabled_ReportsIt()
    {
        _experimentalOptions = new AIExperimentalOptions { ImageGeneration = true };

        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        var enabled = GetData(result, AIUsageTelemetryConstants.ExperimentalFeatures).ShouldBeAssignableTo<IEnumerable<string>>()!;
        enabled.ShouldContain(nameof(AIExperimentalOptions.ImageGeneration));
    }

    [Fact]
    public void GetInformation_WhenExperimentalFeatureDisabled_ReportsEmptySet()
    {
        _experimentalOptions = new AIExperimentalOptions { ImageGeneration = false };

        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        var enabled = GetData(result, AIUsageTelemetryConstants.ExperimentalFeatures).ShouldBeAssignableTo<IEnumerable<string>>()!;
        enabled.ShouldNotContain(nameof(AIExperimentalOptions.ImageGeneration));
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

    // Concrete fakes (not Moq proxies) so AIUsageTelemetryClassification sees them in the
    // "Umbraco."-prefixed test assembly and classifies them as system registrations.

    private sealed class FakeGuardrailEvaluator(string id) : IAIGuardrailEvaluator
    {
        public string Id => id;
        public string Name => id;
        public string Description => string.Empty;
        public AIGuardrailEvaluatorType Type => AIGuardrailEvaluatorType.CodeBased;
        public Type? ConfigType => null;
        public AIEditableModelSchema? GetConfigSchema() => null;
        public Task<AIGuardrailResult> EvaluateAsync(string content, IReadOnlyList<ChatMessage> conversationHistory, AIGuardrailConfig config, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class FakeTestFeature(string id) : IAITestFeature
    {
        public string Id => id;
        public string Name => id;
        public string Description => string.Empty;
        public string Category => "Built-in";
        public Type? ConfigType => null;
        public AIEditableModelSchema? GetConfigSchema() => null;
        public string ExtractOutputValue(AITestTranscript transcript) => throw new NotImplementedException();
        public Task<AITestTranscript> ExecuteAsync(AITest test, int runNumber, Guid? profileIdOverride, IEnumerable<Guid>? contextIdsOverride, IEnumerable<Guid>? guardrailIdsOverride, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class FakeTestGrader(string id) : IAITestGrader
    {
        public string Id => id;
        public string Name => id;
        public string Description => string.Empty;
        public AIGraderType Type => AIGraderType.CodeBased;
        public Type? ConfigType => null;
        public AIEditableModelSchema? GetConfigSchema() => null;
        public Task<AITestGraderResult> GradeAsync(AITestTranscript transcript, AITestOutcome outcome, AITestGraderConfig graderConfig, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
