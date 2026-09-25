// Story DE-3 — Mark an evaluator or grader as needing a capability (AC1-AC5)
//
// STAGED SPEC (task T1). Assumed production surface (ARCHITECTURE "Hiding when the flag is off"):
//   - Umbraco.AI.Core.Models.AIRequiresCapabilityAttribute(AICapability capability),
//     AttributeTargets.Class, AllowMultiple = true.
//   - Umbraco.AI.Extensions.AIRequiresCapabilityExtensions
//       .AreRequiredCapabilitiesEnabled(this object, IAIExperimentalFeatures).
// Uses a real AIExperimentalFeatures (not a mocked IAIExperimentalFeatures) so the specs prove the
// real flags drive the answer, same as EnabledCapabilitiesControllerTests.
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Tests.Unit.Extensions;

public class AIRequiresCapabilityExtensionsTests
{
    private static IAIExperimentalFeatures Flags(bool decision, bool imageGeneration)
    {
        var monitor = new Mock<IOptionsMonitor<AIExperimentalOptions>>();
        monitor.Setup(x => x.CurrentValue).Returns(new AIExperimentalOptions
        {
            Decision = decision,
            ImageGeneration = imageGeneration,
        });
        return new AIExperimentalFeatures(monitor.Object);
    }

    private sealed class Unmarked;

    [AIRequiresCapability(AICapability.Decision)]
    private sealed class NeedsDecision;

    [AIRequiresCapability(AICapability.Decision)]
    [AIRequiresCapability(AICapability.ImageGeneration)]
    private sealed class NeedsDecisionAndImageGeneration;

    #region Happy path

    public class GivenATypeWithNoMarker
    {
        [Fact(Skip = "Pending T1")]
        public void IsAvailableEvenWithEveryExperimentalFlagOff()
            => new Unmarked().AreRequiredCapabilitiesEnabled(Flags(decision: false, imageGeneration: false)).ShouldBeTrue();
    }

    public class GivenATypeNeedingDecisionAndDecisionEnabled
    {
        [Fact(Skip = "Pending T1")]
        public void IsAvailable()
            => new NeedsDecision().AreRequiredCapabilitiesEnabled(Flags(decision: true, imageGeneration: false)).ShouldBeTrue();
    }

    public class GivenATypeNeedingTwoCapabilitiesAndBothEnabled
    {
        [Fact(Skip = "Pending T1")]
        public void IsAvailable()
            => new NeedsDecisionAndImageGeneration().AreRequiredCapabilitiesEnabled(Flags(decision: true, imageGeneration: true)).ShouldBeTrue();
    }

    #endregion

    #region Sad path

    public class GivenATypeNeedingDecisionAndDecisionDisabled
    {
        [Fact(Skip = "Pending T1")]
        public void IsHidden()
            => new NeedsDecision().AreRequiredCapabilitiesEnabled(Flags(decision: false, imageGeneration: true)).ShouldBeFalse();
    }

    public class GivenATypeNeedingTwoCapabilitiesAndOnlyOneEnabled
    {
        [Fact(Skip = "Pending T1")]
        public void IsHidden()
            => new NeedsDecisionAndImageGeneration().AreRequiredCapabilitiesEnabled(Flags(decision: true, imageGeneration: false)).ShouldBeFalse();
    }

    #endregion
}
