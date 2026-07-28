using Umbraco.AI.Extensions;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// <c>output_config.effort</c> is not accepted by the older Claude models, and its <c>xhigh</c> and
/// <c>max</c> levels are accepted by fewer models than the rest. These predicates are the single source
/// for both the per-model declaration surfaced to the profile editor and the decision to send a level at
/// all, so they are worth pinning down.
/// </summary>
/// <remarks>
/// Model IDs and per-model support taken from Anthropic's models overview and effort docs (July 2026).
/// </remarks>
public class AnthropicEffortSupportTests
{
    [Theory]
    [InlineData("claude-opus-4-5-20251101")]
    [InlineData("claude-opus-4-6")]
    [InlineData("claude-sonnet-4-6")]
    [InlineData("claude-opus-4-7")]
    [InlineData("claude-opus-4-8")]
    [InlineData("claude-opus-5")]
    [InlineData("claude-sonnet-5")]
    [InlineData("claude-fable-5")]
    [InlineData("claude-mythos-5")]
    [InlineData("claude-mythos-preview")]
    [InlineData("some-future-claude-model")]
    public void SupportsEffort_ModelAcceptingEffort_ReturnsTrue(string modelId)
    {
        // A deny-list of legacy models, so anything newer — including models this package has not heard
        // of — is treated as supporting effort.
        AnthropicModelUtilities.SupportsEffort(modelId).ShouldBeTrue();
    }

    [Theory]
    [InlineData("claude-3-5-sonnet-20241022")]
    [InlineData("claude-3-7-sonnet-20250219")]
    [InlineData("claude-sonnet-4-20250514")]
    [InlineData("claude-opus-4-20250514")]
    [InlineData("claude-opus-4-1-20250805")]
    [InlineData("claude-sonnet-4-5-20250929")]
    [InlineData("claude-haiku-4-5-20251001")]
    [InlineData(null)]
    [InlineData("")]
    public void SupportsEffort_ModelRejectingEffort_ReturnsFalse(string? modelId)
    {
        AnthropicModelUtilities.SupportsEffort(modelId).ShouldBeFalse();
    }

    [Theory]
    [InlineData("claude-opus-5", "low")]
    [InlineData("claude-opus-5", "medium")]
    [InlineData("claude-opus-5", "high")]
    [InlineData("claude-opus-4-5-20251101", "high")]
    [InlineData("claude-sonnet-4-6", "medium")]
    public void SupportsEffortLevel_BaseLevelsOnAnEffortModel_ReturnsTrue(string modelId, string level)
    {
        AnthropicModelUtilities.SupportsEffortLevel(modelId, level).ShouldBeTrue();
    }

    [Theory]
    [InlineData("claude-opus-4-7")]
    [InlineData("claude-opus-4-8")]
    [InlineData("claude-opus-5")]
    [InlineData("claude-sonnet-5")]
    [InlineData("claude-fable-5")]
    [InlineData("claude-mythos-5")]
    public void SupportsEffortLevel_Xhigh_OnModelsThatHaveIt_ReturnsTrue(string modelId)
    {
        AnthropicModelUtilities.SupportsEffortLevel(modelId, "xhigh").ShouldBeTrue();
    }

    [Theory]
    [InlineData("claude-opus-4-5-20251101")]
    [InlineData("claude-opus-4-6")]
    [InlineData("claude-sonnet-4-6")]
    [InlineData("claude-mythos-preview")]
    public void SupportsEffortLevel_Xhigh_OnModelsWithoutIt_ReturnsFalse(string modelId)
    {
        // xhigh is newer than effort itself: some models that support max don't support xhigh.
        AnthropicModelUtilities.SupportsEffortLevel(modelId, "xhigh").ShouldBeFalse();
    }

    [Theory]
    [InlineData("claude-opus-4-6", true)]
    [InlineData("claude-sonnet-4-6", true)]
    [InlineData("claude-opus-5", true)]
    [InlineData("claude-mythos-preview", true)]
    [InlineData("claude-opus-4-5-20251101", false)]
    public void SupportsEffortLevel_Max_FollowsTheFourSixCutoff(string modelId, bool expected)
    {
        // max arrived with the 4.6 generation, so Opus 4.5 is the one effort-capable model without it.
        AnthropicModelUtilities.SupportsEffortLevel(modelId, "max").ShouldBe(expected);
    }

    [Theory]
    [InlineData("claude-haiku-4-5-20251001", "high")]
    [InlineData("claude-sonnet-4-5-20250929", "low")]
    public void SupportsEffortLevel_ModelWithoutEffort_ReturnsFalseForEveryLevel(string modelId, string level)
    {
        AnthropicModelUtilities.SupportsEffortLevel(modelId, level).ShouldBeFalse();
    }

    [Fact]
    public void SupportsEffortLevel_UnrecognisedLevel_ReturnsFalse()
    {
        AnthropicModelUtilities.SupportsEffortLevel("claude-opus-5", "turbo").ShouldBeFalse();
    }
}
