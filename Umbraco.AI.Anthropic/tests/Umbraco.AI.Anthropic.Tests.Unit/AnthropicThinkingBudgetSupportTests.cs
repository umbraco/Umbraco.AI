using Umbraco.AI.Extensions;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// The thinking budget is rejected with a 400 by the newest Claude families, the same way the sampling
/// parameters are. This predicate is the single source for both the per-model declaration surfaced to the
/// profile editor and the decision to send the budget at all, so it is worth pinning down.
/// </summary>
public class AnthropicThinkingBudgetSupportTests
{
    [Theory]
    [InlineData("claude-sonnet-4-20250514")]
    [InlineData("claude-sonnet-4-5-20250929")]
    [InlineData("claude-opus-4-1-20250805")]
    [InlineData("claude-3-7-sonnet-20250219")]
    public void SupportsThinkingBudget_ModelAcceptingABudget_ReturnsTrue(string modelId)
    {
        AnthropicModelUtilities.SupportsThinkingBudget(modelId).ShouldBeTrue();
    }

    [Theory]
    [InlineData("claude-opus-4-7-20260101")]
    [InlineData("claude-opus-4-8-20260601")]
    [InlineData("claude-opus-5-20260601")]
    [InlineData("claude-sonnet-5-20260601")]
    [InlineData("claude-opus-5-latest")]
    public void SupportsThinkingBudget_ModelRejectingABudget_ReturnsFalse(string modelId)
    {
        AnthropicModelUtilities.SupportsThinkingBudget(modelId).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("some-future-claude-model")]
    public void SupportsThinkingBudget_UnknownOrUnresolvedModel_ReturnsTrue(string? modelId)
    {
        // A deny list: unknown models keep the long-standing behaviour rather than losing the setting
        // the moment Anthropic ships a model this package hasn't heard of.
        AnthropicModelUtilities.SupportsThinkingBudget(modelId).ShouldBeTrue();
    }
}
