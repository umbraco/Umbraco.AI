using Umbraco.AI.Extensions;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// <c>output_config.effort</c> is not accepted by the older Claude models. These predicates are the single
/// source for both the per-model declaration surfaced to the profile editor and the decision to send a
/// level at all, so they are worth pinning down.
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
    [InlineData("xhigh")]
    [InlineData("max")]
    public void SupportsEffortLevel_XhighOrMax_ReturnsFalseEvenOnModelsThatAcceptThem(string level)
    {
        // Neither level is offered: which models accept them cannot be tracked with a hard-coded list —
        // the set with xhigh grows with each release — so a stored value is skipped rather than guessed at.
        // Adding them means reading the models endpoint's per-model capabilities.effort.
        AnthropicModelUtilities.SupportsEffortLevel("claude-opus-5", level).ShouldBeFalse();
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
