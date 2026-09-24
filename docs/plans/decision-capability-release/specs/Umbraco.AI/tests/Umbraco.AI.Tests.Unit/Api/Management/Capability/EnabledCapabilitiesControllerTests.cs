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

        [Theory(Skip = "Pending T8")]
        [InlineData("Chat")]
        [InlineData("Embedding")]
        [InlineData("SpeechToText")]
        public void IncludesStableCapabilities(string capability) => _enabled.ShouldContain(capability);

        [Fact(Skip = "Pending T8")]
        public void ExcludesImageGeneration() => _enabled.ShouldNotContain("ImageGeneration");

        [Fact(Skip = "Pending T8")]
        public void ExcludesDecision() => _enabled.ShouldNotContain("Decision");

        [Fact(Skip = "Pending T8")]
        public void ExcludesReservedModeration() => _enabled.ShouldNotContain("Moderation");

        [Fact(Skip = "Pending T8")]
        public void ExcludesReservedMedia() => _enabled.ShouldNotContain("Media");
    }

    public class GivenTheDecisionFlagOn
    {
        private readonly IReadOnlyList<string> _enabled = GetEnabled(new AIExperimentalOptions { Decision = true });

        [Fact(Skip = "Pending T8")]
        public void IncludesDecision() => _enabled.ShouldContain("Decision");

        [Fact(Skip = "Pending T8")]
        public void StillExcludesImageGeneration() => _enabled.ShouldNotContain("ImageGeneration");
    }

    public class GivenTheImageGenerationFlagOn
    {
        [Fact(Skip = "Pending T8")]
        public void IncludesImageGeneration()
            => GetEnabled(new AIExperimentalOptions { ImageGeneration = true }).ShouldContain("ImageGeneration");
    }

    #endregion
}
