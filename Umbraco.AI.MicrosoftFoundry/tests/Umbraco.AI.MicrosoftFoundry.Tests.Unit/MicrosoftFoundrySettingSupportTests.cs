using Umbraco.AI.Extensions;

namespace Umbraco.AI.MicrosoftFoundry.Tests.Unit;

/// <summary>
/// Foundry fronts other vendors' models, so it inherits their per-model restrictions without owning any of
/// them. These predicates are the single source for both the per-model declaration the profile editor reads
/// and the request-time filter the capability bases apply, so the table is worth pinning.
/// </summary>
/// <remarks>
/// Model names taken from Azure AI Foundry's model catalogue and the vendors' own model overviews (July
/// 2026).
/// </remarks>
public class MicrosoftFoundrySettingSupportTests
{
    [Theory]
    // Azure's GPT deployments, including the undotted GPT-3.5 spelling Azure uses.
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-4.1")]
    [InlineData("gpt-4-32k")]
    [InlineData("gpt-35-turbo")]
    [InlineData("gpt-35-turbo-16k")]
    [InlineData("chatgpt-4o-latest")]
    // Claude families that still accept them.
    [InlineData("claude-3-5-sonnet-20241022")]
    [InlineData("claude-sonnet-4-6")]
    // Vendors with no sampling restriction at all, which is most of the catalogue.
    [InlineData("mistral-large-2411")]
    [InlineData("Llama-3.3-70B-Instruct")]
    [InlineData("Phi-4")]
    [InlineData("cohere-command-r-plus")]
    [InlineData("DeepSeek-R1")]
    // A deployment name that says nothing about what it fronts keeps today's behaviour.
    [InlineData("prod-chat-1")]
    // No model resolved means the capability's default is used, which accepts them.
    [InlineData(null)]
    [InlineData("")]
    public void SupportsSamplingParameters_ModelAcceptingThem_ReturnsTrue(string? modelId)
    {
        MicrosoftFoundryModelUtilities.SupportsSamplingParameters(modelId).ShouldBeTrue();
    }

    [Theory]
    // OpenAI's reasoning models reject a non-default temperature rather than ignoring it.
    [InlineData("o1")]
    [InlineData("o1-mini")]
    [InlineData("o3")]
    [InlineData("o3-mini")]
    [InlineData("o4-mini")]
    [InlineData("gpt-5")]
    [InlineData("gpt-5.6-sol")]
    // Anthropic removed them from Claude Opus 4.7 onwards.
    [InlineData("claude-opus-4-7")]
    [InlineData("claude-opus-4-8")]
    [InlineData("claude-opus-5")]
    // A name that reads as a restrictive vendor's but is not recognised fails safe, which is the
    // allow-list behaviour the first-party packages have — kept exactly where it is warranted.
    [InlineData("gpt-6-turbo")]
    [InlineData("claude-opus-4-9")]
    public void SupportsSamplingParameters_ModelRejectingThem_ReturnsFalse(string modelId)
    {
        MicrosoftFoundryModelUtilities.SupportsSamplingParameters(modelId).ShouldBeFalse();
    }

    [Fact]
    public void SupportsSamplingParameters_DeploymentNameHidingARestrictedModel_ReadsTheUnderlyingModel()
    {
        // The deployments API path: the ID is a user-chosen name, so the decision has to come from what
        // the deployment fronts. This is the case the whole metadata plumbing exists for.
        MicrosoftFoundryModelUtilities
            .SupportsSamplingParameters("prod-chat", modelName: "o3", publisher: "OpenAI")
            .ShouldBeFalse();
    }

    [Fact]
    public void SupportsSamplingParameters_DeploymentNameHidingAnAcceptingModel_ReadsTheUnderlyingModel()
    {
        MicrosoftFoundryModelUtilities
            .SupportsSamplingParameters("prod-chat", modelName: "gpt-4o", publisher: "OpenAI")
            .ShouldBeTrue();
    }

    [Theory]
    [InlineData("Meta")]
    [InlineData("Mistral AI")]
    [InlineData("Microsoft")]
    public void SupportsSamplingParameters_UnrestrictedPublisher_IgnoresAMisleadingName(string publisher)
    {
        // A publisher can only rule a restriction out, never in: a Llama deployment a user named after
        // OpenAI's o3 must not inherit o3's restriction.
        MicrosoftFoundryModelUtilities
            .SupportsSamplingParameters("o3-llama", modelName: "o3-llama", publisher: publisher)
            .ShouldBeTrue();
    }

    [Fact]
    public void SupportsSamplingParameters_RestrictiveVendorPublisherWithAnUnknownName_StaysUnrestricted()
    {
        // Knowing the vendor but not the model would mean guessing, and guessing "restricted" would drop a
        // value from a deployment that works today. So the family patterns decide, and an unrecognisable
        // name reaches neither of them.
        MicrosoftFoundryModelUtilities
            .SupportsSamplingParameters("prod-chat", modelName: "prod-chat", publisher: "OpenAI")
            .ShouldBeTrue();
    }

    [Theory]
    [InlineData("text-embedding-3-small")]
    [InlineData("text-embedding-3-large")]
    // Not an OpenAI embedding name, so OpenAI's rule does not apply and today's behaviour is kept.
    [InlineData("cohere-embed-v3-english")]
    [InlineData("embeddings-prod")]
    [InlineData(null)]
    public void SupportsDimensions_ModelAcceptingIt_ReturnsTrue(string? modelId)
    {
        MicrosoftFoundryModelUtilities.SupportsDimensions(modelId).ShouldBeTrue();
    }

    [Theory]
    // Shortened embeddings are a text-embedding-3 feature; ada-002 predates it. Note the asymmetry with
    // the sampling case: here the *older* model is the restricted one, so an unrecognised
    // text-embedding-* name reads as unsupported.
    [InlineData("text-embedding-ada-002")]
    [InlineData("text-embedding-4-preview")]
    public void SupportsDimensions_ModelRejectingIt_ReturnsFalse(string modelId)
    {
        MicrosoftFoundryModelUtilities.SupportsDimensions(modelId).ShouldBeFalse();
    }

    [Fact]
    public void SupportsDimensions_DeploymentNameHidingAda002_ReadsTheUnderlyingModel()
    {
        MicrosoftFoundryModelUtilities
            .SupportsDimensions("emb-prod", modelName: "text-embedding-ada-002", publisher: "OpenAI")
            .ShouldBeFalse();
    }

    [Fact]
    public void FormatDisplayName_NothingButAnId_ReturnsTheIdUnchanged()
    {
        // The models API path reports only an ID, so there is nothing to add.
        MicrosoftFoundryModelUtilities.FormatDisplayName("gpt-4o").ShouldBe("gpt-4o");
    }

    [Fact]
    public void FormatDisplayName_DeploymentFrontingAKnownModel_ShowsBoth()
    {
        // The deployment name is still shown because it is the value stored on the profile.
        MicrosoftFoundryModelUtilities
            .FormatDisplayName("prod-chat", "gpt-4o", "2024-11-20")
            .ShouldBe("gpt-4o 2024-11-20 (prod-chat)");
    }

    [Fact]
    public void FormatDisplayName_DeploymentNamedAfterItsModel_DoesNotRepeatItself()
    {
        MicrosoftFoundryModelUtilities.FormatDisplayName("gpt-4o", "gpt-4o", null).ShouldBe("gpt-4o");
    }

    [Fact]
    public void FormatDisplayName_KnownModelWithNoVersion_OmitsTheVersion()
    {
        MicrosoftFoundryModelUtilities
            .FormatDisplayName("prod-chat", "gpt-4o", null)
            .ShouldBe("gpt-4o (prod-chat)");
    }
}
