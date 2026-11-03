// EX-3 — Extraction stays hidden until turned on (AC8: the enabled-capabilities list)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental Extraction flag

// Copies EnabledCapabilitiesControllerTests: a real AIExperimentalFeatures (not a mocked
// IAIExperimentalFeatures), so the specs prove the real flag gates the list.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Capability.Controllers;

namespace Umbraco.AI.Tests.Unit.Api.Management.Capability;

public class EnabledCapabilitiesExtractionTests
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

    public class GivenTheExtractionCapability
    {
        [Fact]
        public void IsInTheImplementedSet()
            => EnabledCapabilitiesController.ImplementedCapabilities.ShouldContain(AICapability.Extraction);
    }

    public class GivenTheExtractionFlagOn
    {
        private readonly IReadOnlyList<string> _enabled = GetEnabled(new AIExperimentalOptions { Extraction = true });

        [Fact]
        public void IncludesExtraction() => _enabled.ShouldContain("Extraction");

        [Fact]
        public void StillExcludesDecision() => _enabled.ShouldNotContain("Decision");
    }

    #endregion

    #region Sad path

    public class GivenEveryExperimentalFlagOff
    {
        [Fact]
        public void ExcludesExtraction() => GetEnabled(new AIExperimentalOptions()).ShouldNotContain("Extraction");
    }

    public class GivenOnlyTheDecisionFlagOn
    {
        [Fact]
        public void StillExcludesExtraction()
            => GetEnabled(new AIExperimentalOptions { Decision = true }).ShouldNotContain("Extraction");
    }

    #endregion
}
