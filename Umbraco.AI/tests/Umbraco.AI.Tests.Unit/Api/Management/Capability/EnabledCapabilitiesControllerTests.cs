// DR-6 — Hide disabled experimental capabilities (AC1, AC2)
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Capability.Controllers;

namespace Umbraco.AI.Tests.Unit.Api.Management.Capability;

// Uses a real AIExperimentalFeatures (not a mocked IAIExperimentalFeatures) so the specs
// prove the real flags gate the list, per the add-ai-capability skill.
public class EnabledCapabilitiesControllerTests
{
    private static IReadOnlyList<string> GetEnabled(AIExperimentalOptions options)
    {
        var monitor = new Mock<IOptionsMonitor<AIExperimentalOptions>>();
        monitor.Setup(x => x.CurrentValue).Returns(options);
        var controller = new EnabledCapabilitiesController(new AIExperimentalFeatures(monitor.Object));

        var result = controller.GetEnabledCapabilities().GetAwaiter().GetResult();
        return ((IEnumerable<string>)((OkObjectResult)result.Result!).Value!).ToList();
    }

    #region Happy path

    public class GivenBothExperimentalFlagsOff
    {
        private readonly IReadOnlyList<string> _enabled = GetEnabled(new AIExperimentalOptions());

        [Theory]
        [InlineData("Chat")]
        [InlineData("Embedding")]
        [InlineData("SpeechToText")]
        public void IncludesStableCapabilities(string capability) => _enabled.ShouldContain(capability);

        [Fact]
        public void ExcludesImageGeneration() => _enabled.ShouldNotContain("ImageGeneration");

        [Fact]
        public void ExcludesDecision() => _enabled.ShouldNotContain("Decision");

        [Fact]
        public void ExcludesReservedModeration() => _enabled.ShouldNotContain("Moderation");

        [Fact]
        public void ExcludesReservedMedia() => _enabled.ShouldNotContain("Media");
    }

    public class GivenTheDecisionFlagOn
    {
        private readonly IReadOnlyList<string> _enabled = GetEnabled(new AIExperimentalOptions { Decision = true });

        [Fact]
        public void IncludesDecision() => _enabled.ShouldContain("Decision");

        [Fact]
        public void StillExcludesImageGeneration() => _enabled.ShouldNotContain("ImageGeneration");
    }

    public class GivenTheImageGenerationFlagOn
    {
        [Fact]
        public void IncludesImageGeneration()
            => GetEnabled(new AIExperimentalOptions { ImageGeneration = true }).ShouldContain("ImageGeneration");
    }

    #endregion
}
