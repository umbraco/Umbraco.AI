using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Tests.Unit.Telemetry;

public class AIExtensionUsageTelemetryProviderTests
{
    private AIUsageTelemetryOptions _telemetryOptions = new();

    private AIExtensionUsageTelemetryProvider CreateProvider()
    {
        var monitor = new Mock<IOptionsMonitor<AIUsageTelemetryOptions>>();
        monitor.Setup(x => x.CurrentValue).Returns(() => _telemetryOptions);

        // FakeTool lives in the "Umbraco.AI."-prefixed test assembly -> system.
        // Moq proxies live in DynamicProxyGenAssembly2 -> custom.
        var customTool = new Mock<IAITool>();
        customTool.Setup(x => x.Id).Returns("acme-erp-sync");

        var customResourceType = new Mock<IAIContextResourceType>();
        customResourceType.Setup(x => x.Id).Returns("acme-product-feed");

        var customChatMiddleware = new Mock<IAIChatMiddleware>();

        // The provider sweeps loaded Umbraco.AI assemblies for AI{Name}Collection types and
        // resolves them from this service provider; unregistered collections are skipped.
        var services = new ServiceCollection();
        services.AddSingleton(new AIToolCollection(() => [new FakeTool(), customTool.Object]));
        services.AddSingleton(new AIContextResourceTypeCollection(() => [customResourceType.Object]));
        services.AddSingleton(new AIChatMiddlewareCollection(() => [customChatMiddleware.Object]));
        services.AddSingleton(new AIEmbeddingMiddlewareCollection(() => []));
        services.AddSingleton(new AISpeechToTextMiddlewareCollection(() => []));

        return new AIExtensionUsageTelemetryProvider(monitor.Object, services.BuildServiceProvider());
    }

    [Fact]
    public void GetInformation_WhenDisabled_ReturnsEmpty()
    {
        _telemetryOptions = new AIUsageTelemetryOptions { Enabled = false };

        CreateProvider().GetInformation().ShouldBeEmpty();
    }

    [Fact]
    public void GetInformation_ReportsRegistrationCountsOnly_NeverIds()
    {
        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        GetData(result, AIUsageTelemetryConstants.ToolCount).ShouldBe(2);
        GetData(result, AIUsageTelemetryConstants.ToolCustomCount).ShouldBe(1);
        GetData(result, AIUsageTelemetryConstants.ContextResourceTypeCount).ShouldBe(1);
        GetData(result, AIUsageTelemetryConstants.ContextResourceTypeCustomCount).ShouldBe(1);

        // Every value is a plain count - extension IDs are never reported
        foreach (UsageInformation info in result)
        {
            info.Data.ShouldBeOfType<int>();
        }
    }

    [Fact]
    public void GetInformation_DiscoversExtensionCollectionsDynamically()
    {
        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        GetData(result, AIUsageTelemetryConstants.ExtensionCustomCount("ChatMiddleware")).ShouldBe(1);
        GetData(result, AIUsageTelemetryConstants.ExtensionCustomCount("EmbeddingMiddleware")).ShouldBe(0);
        GetData(result, AIUsageTelemetryConstants.ExtensionCustomCount("SpeechToTextMiddleware")).ShouldBe(0);
    }

    [Fact]
    public void GetInformation_SkipsCollectionsCoveredByEntityLevelProviders()
    {
        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        // These are reported with in-use system/custom classification elsewhere
        result.ShouldNotContain(i => i.Name == AIUsageTelemetryConstants.ExtensionCustomCount("Provider"));
        result.ShouldNotContain(i => i.Name == AIUsageTelemetryConstants.ExtensionCustomCount("GuardrailEvaluator"));
        result.ShouldNotContain(i => i.Name == AIUsageTelemetryConstants.ExtensionCustomCount("TestFeature"));
        result.ShouldNotContain(i => i.Name == AIUsageTelemetryConstants.ExtensionCustomCount("TestGrader"));
        result.ShouldNotContain(i => i.Name == AIUsageTelemetryConstants.ExtensionCustomCount("AgentSurface"));
    }

    [Fact]
    public void GetInformation_EmitsOnlySafelistedKeys()
    {
        var safelist = typeof(AIUsageTelemetryConstants)
            .GetFields()
            .Select(f => (string)f.GetValue(null)!)
            .ToHashSet();

        UsageInformation[] result = CreateProvider().GetInformation().ToArray();

        result.ShouldNotBeEmpty();
        foreach (UsageInformation info in result)
        {
            // Extension keys are dynamic per discovered collection; everything else must be a constant
            var isExtensionKey = info.Name.StartsWith("UmbracoAI", StringComparison.Ordinal)
                && info.Name.EndsWith("CustomCount", StringComparison.Ordinal);

            (safelist.Contains(info.Name) || isExtensionKey).ShouldBeTrue(
                $"Key '{info.Name}' is not safelisted");
        }
    }

    private static object GetData(UsageInformation[] result, string name)
        => result.Single(i => i.Name == name).Data;
}
