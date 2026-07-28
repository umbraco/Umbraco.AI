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
    [InlineData("low")]
    [InlineData("medium")]
    [InlineData("high")]
    [InlineData("HIGH")]
    [InlineData(" high ")]
    public void IsKnownEffortLevel_OfferedLevel_ReturnsTrue(string level)
    {
        AnthropicModelUtilities.IsKnownEffortLevel(level).ShouldBeTrue();
    }

    [Theory]
    [InlineData("xhigh")]
    [InlineData("max")]
    [InlineData("turbo")]
    [InlineData("")]
    public void IsKnownEffortLevel_LevelNotOffered_ReturnsFalse(string level)
    {
        // xhigh and max reach a subset of models that a hard-coded list cannot track, so a value stored
        // by an API caller is skipped rather than guessed at.
        AnthropicModelUtilities.IsKnownEffortLevel(level).ShouldBeFalse();
    }
}
