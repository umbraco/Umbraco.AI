using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Providers;

/// <summary>
/// Covers the per-model setting declarations: a capability declares which settings apply to a model, the
/// bases project that into the model list's metadata, and consumers read it back as a tri-state.
/// </summary>
public class CapabilitySettingSupportTests
{
    private static readonly FakeProviderSettings Settings = new();

    #region Projection into model metadata

    [Fact]
    public async Task GetModelsAsync_CapabilityDeclaresSupport_ProjectsDeclarationIntoMetadata()
    {
        // Arrange
        IAICapability capability = new DeclaringChatCapability();

        // Act
        var models = await capability.GetModelsAsync(Settings);

        // Assert — the supporting model carries the positive declaration, the other the negative
        var supporting = models.Single(m => m.Model.ModelId == "reasoning-model");
        supporting.Metadata[AIModelMetadataKeys.CapabilitySettingsSupported].ShouldBe("reasoningEffort");
        supporting.GetCapabilitySettingSupport("reasoningEffort").ShouldBe(AISettingSupport.Supported);

        var plain = models.Single(m => m.Model.ModelId == "plain-model");
        plain.Metadata[AIModelMetadataKeys.CapabilitySettingsUnsupported].ShouldBe("reasoningEffort");
        plain.GetCapabilitySettingSupport("reasoningEffort").ShouldBe(AISettingSupport.Unsupported);
    }

    [Fact]
    public async Task GetModelsAsync_CapabilityDeclaresSupport_PreservesProviderOwnMetadata()
    {
        // Arrange — the provider already puts its own constraints on the descriptor
        IAICapability capability = new DeclaringChatCapability();

        // Act
        var models = await capability.GetModelsAsync(Settings);

        // Assert
        var supporting = models.Single(m => m.Model.ModelId == "reasoning-model");
        supporting.Metadata["custom.key"].ShouldBe("custom-value");
        supporting.Name.ShouldBe("Reasoning Model");
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
        var supporting = models.Single(m => m.Model.ModelId == "reasoning-model");
        supporting.Metadata[AIModelMetadataKeys.CapabilitySettingsSupported].ShouldBe("reasoningEffort");
    }

    #endregion

    #region Reading support back

    [Fact]
    public void GetCapabilitySettingSupport_NoMetadata_ReturnsUnknown()
    {
        var model = new AIModelDescriptor(new AIModelRef("test", "some-model"), "Some Model");

        model.GetCapabilitySettingSupport("reasoningEffort").ShouldBe(AISettingSupport.Unknown);
    }

    [Fact]
    public void GetCapabilitySettingSupport_KeyNotDeclared_ReturnsUnknown()
    {
        var model = Describe(new Dictionary<string, string>
        {
            [AIModelMetadataKeys.CapabilitySettingsSupported] = "reasoningEffort",
        });

        model.GetCapabilitySettingSupport("thinkingBudgetTokens").ShouldBe(AISettingSupport.Unknown);
    }

    [Fact]
    public void GetCapabilitySettingSupport_ListedInBoth_PrefersUnsupported()
    {
        var model = Describe(new Dictionary<string, string>
        {
            [AIModelMetadataKeys.CapabilitySettingsSupported] = "reasoningEffort",
            [AIModelMetadataKeys.CapabilitySettingsUnsupported] = "reasoningEffort",
        });

        model.GetCapabilitySettingSupport("reasoningEffort").ShouldBe(AISettingSupport.Unsupported);
    }

    [Theory]
    [InlineData("reasoningEffort")]
    [InlineData("ReasoningEffort")]
    [InlineData(" reasoningEffort ")]
    public void GetCapabilitySettingSupport_KeyCasingAndWhitespace_StillMatches(string fieldKey)
    {
        var model = Describe(new Dictionary<string, string>
        {
            [AIModelMetadataKeys.CapabilitySettingsSupported] = "verbosity, reasoningEffort",
        });

        model.GetCapabilitySettingSupport(fieldKey).ShouldBe(AISettingSupport.Supported);
    }

    #endregion

    private static AIModelDescriptor Describe(IReadOnlyDictionary<string, string> metadata)
        => new(new AIModelRef("test", "some-model"), "Some Model", metadata);

    /// <summary>
    /// A capability that declares reasoning-effort support for one of its two models, exercising the
    /// projection performed by <see cref="AICapabilityBase{TSettings}"/>.
    /// </summary>
    private sealed class DeclaringChatCapability()
        : AIChatCapabilityBase<FakeProviderSettings, DeclaringChatCapability.CapabilitySettings>(
            new FakeAIProvider())
    {
        public override AIModelSettingSupport GetSettingSupport(string modelId)
            => modelId == "reasoning-model"
                ? new AIModelSettingSupport
                {
                    SupportedCapabilitySettings = [nameof(CapabilitySettings.ReasoningEffort)],
                }
                : new AIModelSettingSupport
                {
                    UnsupportedCapabilitySettings = [nameof(CapabilitySettings.ReasoningEffort)],
                };

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>(
            [
                new AIModelDescriptor(
                    new AIModelRef("test", "reasoning-model"),
                    "Reasoning Model",
                    new Dictionary<string, string> { ["custom.key"] = "custom-value" }),
                new AIModelDescriptor(new AIModelRef("test", "plain-model"), "Plain Model"),
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
