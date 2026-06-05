using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Core.Tools;

namespace Umbraco.AI.Tests.Unit.Telemetry;

public class AIUsageTelemetryClassificationTests
{
    [Fact]
    public void IsSystemType_WithUmbracoAiAssembly_ReturnsTrue()
    {
        // Umbraco.AI.Core
        AIUsageTelemetryClassification.IsSystemType(typeof(AIUsageTelemetryOptions)).ShouldBeTrue();

        // Umbraco.AI.Tests.Unit (this assembly) - "Umbraco.AI." prefixed
        AIUsageTelemetryClassification.IsSystemType(typeof(AIUsageTelemetryClassificationTests)).ShouldBeTrue();
    }

    [Fact]
    public void IsSystemType_WithOtherUmbracoAssembly_ReturnsFalse()
    {
        // Umbraco.Cms.Core starts with "Umbraco." but is NOT an Umbraco.AI assembly.
        // This pins the strictness of the rule: a broad "Umbraco." prefix would wrongly
        // classify community packages (conventionally "Umbraco.Community.*") as system.
        AIUsageTelemetryClassification.IsSystemType(typeof(Umbraco.Cms.Core.Models.UsageInformation)).ShouldBeFalse();
    }

    [Fact]
    public void IsSystemType_WithNonUmbracoAssembly_ReturnsFalse()
    {
        // Moq proxies live in DynamicProxyGenAssembly2
        var proxy = new Mock<IAITool>().Object;

        AIUsageTelemetryClassification.IsSystemType(proxy.GetType()).ShouldBeFalse();
        AIUsageTelemetryClassification.IsSystemType(typeof(string)).ShouldBeFalse();
    }
}
