using Umbraco.AI.Core.Telemetry;

namespace Umbraco.AI.Tests.Unit.Telemetry;

public class AIModelFamilyNormalizerTests
{
    [Theory]
    [InlineData("openai", "gpt-4o-mini", "openai/gpt-4o")]
    [InlineData("openai", "gpt-4-turbo", "openai/gpt-4")]
    [InlineData("openai", "o3-mini", "openai/o3")]
    [InlineData("anthropic", "claude-sonnet-4-5", "anthropic/claude-sonnet")]
    [InlineData("anthropic", "claude-3-5-haiku-latest", "anthropic/claude-haiku")]
    [InlineData("google", "gemini-2.5-pro", "google/gemini")]
    [InlineData("mistral", "mistral-large-latest", "mistral/mistral")]
    [InlineData("deepseek", "deepseek-chat", "deepseek/deepseek")]
    [InlineData("amazon", "amazon.nova-pro-v1:0", "amazon/nova")]
    public void Normalize_WithKnownModelFamily_ReturnsProviderAndFamily(string providerId, string modelId, string expected)
    {
        AIModelFamilyNormalizer.Normalize(providerId, modelId).ShouldBe(expected);
    }

    [Fact]
    public void Normalize_RespectsTokenBoundaries()
    {
        // "gpt-4o" must not be reported as the "gpt-4" family
        AIModelFamilyNormalizer.Normalize("openai", "gpt-4o").ShouldBe("openai/gpt-4o");
    }

    [Fact]
    public void Normalize_IsCaseInsensitive()
    {
        AIModelFamilyNormalizer.Normalize("OpenAI", "GPT-4o").ShouldBe("openai/gpt-4o");
    }

    [Theory]
    [InlineData("microsoftfoundry", "acme-merger-faq-deployment")]
    [InlineData("huggingface", "internal-finetune-v2")]
    public void Normalize_WithUserAuthoredModelName_ReturnsOther(string providerId, string modelId)
    {
        // User-authored deployment/model names can encode business information and must
        // never be reported verbatim.
        var result = AIModelFamilyNormalizer.Normalize(providerId, modelId);

        result.ShouldBe($"{providerId}/other");
        result.ShouldNotContain("acme");
        result.ShouldNotContain("finetune");
    }
}
