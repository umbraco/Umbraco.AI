using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Providers;

/// <summary>
/// Covers the per-model setting declarations: a capability names the settings a model rejects, the bases
/// project that into the model list's metadata, and consumers read it back.
/// </summary>
public class CapabilitySettingSupportTests
{
    private static readonly FakeProviderSettings Settings = new();

    #region Projection into model metadata

    [Fact]
    public async Task GetModelsAsync_ModelRejectsSetting_ProjectsDeclarationIntoMetadata()
    {
        // Arrange
        IAICapability capability = new DeclaringChatCapability();

        // Act
        var models = await capability.GetModelsAsync(Settings);

        // Assert
        var plain = models.Single(m => m.Model.ModelId == "plain-model");
        plain.Metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldBe("reasoningEffort");
        plain.IsCapabilitySettingSupported("reasoningEffort").ShouldBeFalse();
    }

    [Fact]
    public async Task GetModelsAsync_ModelAcceptsSetting_DeclaresNothingForIt()
    {
        // Arrange
        IAICapability capability = new DeclaringChatCapability();

        // Act
        var models = await capability.GetModelsAsync(Settings);

        // Assert — silence, not a positive claim: the key is simply absent
        var supporting = models.Single(m => m.Model.ModelId == "reasoning-model");
        supporting.Metadata.ContainsKey(AIModelMetadataKeys.CapabilitySettingsUnsupported).ShouldBeFalse();
        supporting.IsCapabilitySettingSupported("reasoningEffort").ShouldBeTrue();
    }

    [Fact]
    public async Task GetModelsAsync_ModelWithOwnMetadata_PreservesIt()
    {
        // Arrange — the provider already puts its own constraints on the descriptor
        IAICapability capability = new DeclaringChatCapability();

        // Act
        var models = await capability.GetModelsAsync(Settings);

        // Assert
        var plain = models.Single(m => m.Model.ModelId == "plain-model");
        plain.Metadata["custom.key"].ShouldBe("custom-value");
        plain.Name.ShouldBe("Plain Model");
    }

    [Fact]
    public async Task GetModelsAsync_CapabilityDeclaresNothing_ReturnsDescriptorsUntouched()
    {
        // Arrange — a capability on the base that doesn't override the declaration hook
        var capabilityImpl = new PlainChatCapability();
        IAICapability capability = capabilityImpl;

        // Act
        var models = await capability.GetModelsAsync(Settings);

        // Assert — no declaration means no metadata is invented, and the original list is passed through
        models.ShouldBeSameAs(capabilityImpl.Models);
        models.ShouldAllBe(m => m.Metadata.Count == 0);
    }

    [Fact]
    public async Task GetModelsAsync_DeclarationUsesPropertyName_NormalisesToSchemaFieldKey()
    {
        // Arrange — declared as nameof(...) i.e. "ReasoningEffort", read back as the camelCase field key
        IAICapability capability = new DeclaringChatCapability();

        // Act
        var models = await capability.GetModelsAsync(Settings);

        // Assert
        var plain = models.Single(m => m.Model.ModelId == "plain-model");
        plain.Metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldBe("reasoningEffort");
    }

    #endregion

    #region Reading declarations back

    [Fact]
    public void IsCapabilitySettingSupported_NoMetadata_ReturnsTrue()
    {
        var model = new AIModelDescriptor(new AIModelRef("test", "some-model"), "Some Model");

        model.IsCapabilitySettingSupported("reasoningEffort").ShouldBeTrue();
    }

    [Fact]
    public void IsCapabilitySettingSupported_DifferentKeyDeclared_ReturnsTrue()
    {
        var model = Describe("thinkingBudgetTokens");

        model.IsCapabilitySettingSupported("reasoningEffort").ShouldBeTrue();
    }

    [Theory]
    [InlineData("reasoningEffort")]
    [InlineData("ReasoningEffort")]
    [InlineData(" reasoningEffort ")]
    public void IsCapabilitySettingSupported_DeclaredKeyRegardlessOfCasingOrWhitespace_ReturnsFalse(string fieldKey)
    {
        var model = Describe("verbosity, reasoningEffort");

        model.IsCapabilitySettingSupported(fieldKey).ShouldBeFalse();
    }

    #endregion

    private static AIModelDescriptor Describe(string unsupported)
        => new(
            new AIModelRef("test", "some-model"),
            "Some Model",
            new Dictionary<string, string> { [AIModelMetadataKeys.CapabilitySettingsUnsupported] = unsupported });

    /// <summary>
    /// A capability that rejects reasoning effort on one of its two models, exercising the projection
    /// performed by <see cref="AICapabilityBase{TSettings}"/>.
    /// </summary>
    private sealed class DeclaringChatCapability()
        : AIChatCapabilityBase<FakeProviderSettings, DeclaringChatCapability.CapabilitySettings>(
            new FakeAIProvider())
    {
        public override AIModelSettingSupport GetSettingSupport(string modelId)
            => modelId == "reasoning-model"
                ? AIModelSettingSupport.Default
                : new AIModelSettingSupport
                {
                    UnsupportedCapabilitySettings = [nameof(CapabilitySettings.ReasoningEffort)],
                };

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>(
            [
                new AIModelDescriptor(new AIModelRef("test", "reasoning-model"), "Reasoning Model"),
                new AIModelDescriptor(
                    new AIModelRef("test", "plain-model"),
                    "Plain Model",
                    new Dictionary<string, string> { ["custom.key"] = "custom-value" }),
            ]);

        protected override IChatClient CreateClient(FakeProviderSettings settings, string? modelId)
            => throw new NotSupportedException();

        protected override void ApplyCapabilitySettings(
            CapabilitySettings capabilitySettings,
            string? modelId,
            ChatOptions options)
            => throw new NotSupportedException();

        internal sealed class CapabilitySettings
        {
            public string? ReasoningEffort { get; set; }
        }
    }

    /// <summary>
    /// A capability that declares nothing, so the projection must leave its descriptors alone.
    /// </summary>
    private sealed class PlainChatCapability()
        : AIChatCapabilityBase<FakeProviderSettings>(new FakeAIProvider())
    {
        public IReadOnlyList<AIModelDescriptor> Models { get; } =
        [
            new AIModelDescriptor(new AIModelRef("test", "plain-model"), "Plain Model"),
        ];

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Models);

        protected override IChatClient CreateClient(FakeProviderSettings settings, string? modelId)
            => throw new NotSupportedException();
    }
}
