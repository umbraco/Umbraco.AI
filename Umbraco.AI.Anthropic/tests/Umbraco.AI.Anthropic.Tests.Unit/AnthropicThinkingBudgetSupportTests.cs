using Umbraco.AI.Extensions;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Manual extended thinking (<c>thinking.type: "enabled"</c> with <c>budget_tokens</c>) is rejected with a
/// 400 by Claude 4.7 and later, which use adaptive thinking and <c>output_config.effort</c> instead. This
/// predicate is the single source for both the per-model declaration surfaced to the profile editor and
/// the decision to send a budget at all, so it is worth pinning down.
/// </summary>
/// <remarks>
/// Model IDs and per-model support taken from Anthropic's models overview and extended-thinking docs
/// (July 2026).
/// </remarks>
public class AnthropicThinkingBudgetSupportTests
{
    [Theory]
    [InlineData("claude-3-7-sonnet-20250219")]
    [InlineData("claude-sonnet-4-20250514")]
    [InlineData("claude-opus-4-20250514")]
    [InlineData("claude-opus-4-1-20250805")]
    [InlineData("claude-opus-4-5-20251101")]
    [InlineData("claude-sonnet-4-5-20250929")]
    [InlineData("claude-haiku-4-5-20251001")]
    [InlineData("claude-opus-4-6")]
    [InlineData("claude-sonnet-4-6")]
    [InlineData("claude-mythos-preview")]
    public void SupportsThinkingBudget_ModelAcceptingABudget_ReturnsTrue(string modelId)
    {
        AnthropicModelUtilities.SupportsThinkingBudget(modelId).ShouldBeTrue();
    }

    [Theory]
    [InlineData("claude-opus-4-7")]
    [InlineData("claude-opus-4-8")]
    [InlineData("claude-opus-5")]
    [InlineData("claude-sonnet-5")]
    [InlineData("claude-fable-5")]
    [InlineData("claude-mythos-5")]
    public void SupportsThinkingBudget_ModelRejectingABudget_ReturnsFalse(string modelId)
    {
        AnthropicModelUtilities.SupportsThinkingBudget(modelId).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("claude-opus-6")]
    [InlineData("claude-sonnet-6")]
    [InlineData("some-future-claude-model")]
    public void SupportsThinkingBudget_UnknownOrUnresolvedModel_ReturnsFalse(string? modelId)
    {
        // An allow-list: everything from Claude 4.7 onwards rejects a budget, so a model this package
        // has not heard of is far more likely to reject it than accept it.
        AnthropicModelUtilities.SupportsThinkingBudget(modelId).ShouldBeFalse();
    }
}
