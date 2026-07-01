using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;

namespace Umbraco.AI.Tests.Unit.Settings;

public class AIExperimentalFeaturesTests
{
    private static AIExperimentalFeatures CreateSut(AIExperimentalOptions options)
    {
        var monitor = new Mock<IOptionsMonitor<AIExperimentalOptions>>();
        monitor.Setup(x => x.CurrentValue).Returns(options);
        return new AIExperimentalFeatures(monitor.Object);
    }

    [Theory]
    [InlineData(AICapability.Chat)]
    [InlineData(AICapability.Embedding)]
    [InlineData(AICapability.SpeechToText)]
    public void IsCapabilityEnabled_NonExperimentalCapability_AlwaysEnabled(AICapability capability)
    {
        var sut = CreateSut(new AIExperimentalOptions { ImageGeneration = false });

        sut.IsCapabilityEnabled(capability).ShouldBeTrue();
    }

    [Fact]
    public void IsCapabilityEnabled_ImageGeneration_DisabledByDefault()
    {
        var sut = CreateSut(new AIExperimentalOptions());

        sut.IsCapabilityEnabled(AICapability.ImageGeneration).ShouldBeFalse();
    }

    [Fact]
    public void IsCapabilityEnabled_ImageGeneration_EnabledWhenFlagSet()
    {
        var sut = CreateSut(new AIExperimentalOptions { ImageGeneration = true });

        sut.IsCapabilityEnabled(AICapability.ImageGeneration).ShouldBeTrue();
    }
}
