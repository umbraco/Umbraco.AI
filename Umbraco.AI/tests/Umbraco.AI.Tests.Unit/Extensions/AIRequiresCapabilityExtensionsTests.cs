// Story DE-3 — Mark an evaluator or grader as needing a capability (AC1-AC5)
//
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
    private class NeedsDecision;

    [AIRequiresCapability(AICapability.Decision)]
    [AIRequiresCapability(AICapability.ImageGeneration)]
    private sealed class NeedsDecisionAndImageGeneration;

    private sealed class SubclassOfNeedsDecisionWithNoAttributeOfItsOwn : NeedsDecision;

    #region Happy path

    public class GivenATypeWithNoMarker
    {
        [Fact]
        public void IsAvailableEvenWithEveryExperimentalFlagOff()
            => new Unmarked().AreRequiredCapabilitiesEnabled(Flags(decision: false, imageGeneration: false)).ShouldBeTrue();
    }

    public class GivenATypeNeedingDecisionAndDecisionEnabled
    {
        [Fact]
        public void IsAvailable()
            => new NeedsDecision().AreRequiredCapabilitiesEnabled(Flags(decision: true, imageGeneration: false)).ShouldBeTrue();
    }

    public class GivenATypeNeedingTwoCapabilitiesAndBothEnabled
    {
        [Fact]
        public void IsAvailable()
            => new NeedsDecisionAndImageGeneration().AreRequiredCapabilitiesEnabled(Flags(decision: true, imageGeneration: true)).ShouldBeTrue();
    }

    public class GivenTheTypeOverloadAndAnEnabledRequirement
    {
        [Fact]
        public void IsAvailable()
            => typeof(NeedsDecision).AreRequiredCapabilitiesEnabled(Flags(decision: true, imageGeneration: false)).ShouldBeTrue();
    }

    #endregion

    #region Sad path

    public class GivenATypeNeedingDecisionAndDecisionDisabled
    {
        [Fact]
        public void IsHidden()
            => new NeedsDecision().AreRequiredCapabilitiesEnabled(Flags(decision: false, imageGeneration: true)).ShouldBeFalse();
    }

    public class GivenATypeNeedingTwoCapabilitiesAndOnlyOneEnabled
    {
        [Fact]
        public void IsHidden()
            => new NeedsDecisionAndImageGeneration().AreRequiredCapabilitiesEnabled(Flags(decision: true, imageGeneration: false)).ShouldBeFalse();
    }

    public class GivenASubclassOfAMarkedTypeWithNoAttributeOfItsOwnAndTheRequirementDisabled
    {
        [Fact]
        public void IsHidden()
            => new SubclassOfNeedsDecisionWithNoAttributeOfItsOwn().AreRequiredCapabilitiesEnabled(Flags(decision: false, imageGeneration: true)).ShouldBeFalse();
    }

    #endregion
}
