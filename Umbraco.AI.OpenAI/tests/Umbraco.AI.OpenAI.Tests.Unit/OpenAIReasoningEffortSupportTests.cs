using Umbraco.AI.Extensions;

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// The reasoning effort applies to the o-series and the GPT-5 line only. This predicate is the single
/// source for both the per-model declaration surfaced to the profile editor and the decision to send an
/// effort at all, so it is worth pinning down.
/// </summary>
/// <remarks>
/// Model IDs taken from OpenAI's models list, reasoning guide and deprecations page (July 2026).
/// </remarks>
public class OpenAIReasoningEffortSupportTests
{
    [Theory]
    [InlineData("gpt-5.6")]
    [InlineData("gpt-5.6-sol")]
    [InlineData("gpt-5.6-terra")]
    [InlineData("gpt-5.6-luna")]
    [InlineData("gpt-5.5")]
    [InlineData("gpt-5.4")]
    [InlineData("gpt-5")]
    [InlineData("o1")]
    [InlineData("o3-mini")]
    [InlineData("o4-mini")]
    public void SupportsReasoningEffort_ReasoningModel_ReturnsTrue(string modelId)
    {
        OpenAIModelUtilities.SupportsReasoningEffort(modelId).ShouldBeTrue();
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-4.1")]
    [InlineData("gpt-4-turbo")]
    [InlineData("gpt-3.5-turbo")]
    [InlineData("chatgpt-4o-latest")]
    [InlineData(null)]
    [InlineData("")]
    public void SupportsReasoningEffort_NonReasoningModel_ReturnsFalse(string? modelId)
    {
        OpenAIModelUtilities.SupportsReasoningEffort(modelId).ShouldBeFalse();
    }

    [Theory]
    [InlineData("gpt-5-chat-latest")]
    [InlineData("gpt-5.6-chat")]
    public void SupportsReasoningEffort_NonReasoningChatVariant_ReturnsFalse(string modelId)
    {
        // Chat variants sit inside the GPT-5 family but are not reasoning models, under either the
        // undotted or dotted naming.
        OpenAIModelUtilities.SupportsReasoningEffort(modelId).ShouldBeFalse();
    }

    [Fact]
    public void SupportsReasoningEffort_UnknownModel_ReturnsFalse()
    {
        // A positive list: a reasoning family released after this package ships reads as unsupported,
        // which hides the setting and skips sending it rather than failing the request.
        OpenAIModelUtilities.SupportsReasoningEffort("gpt-6").ShouldBeFalse();
    }
}
