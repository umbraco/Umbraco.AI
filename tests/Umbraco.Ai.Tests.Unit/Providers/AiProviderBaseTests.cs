using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Settings;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Tests.Unit.Providers;

public class AiProviderBaseTests
{
    private readonly Mock<IAiProviderInfrastructure> _infrastructureMock;
    private readonly Mock<IAiCapabilityFactory> _capabilityFactoryMock;
    private readonly Mock<IAiSettingDefinitionBuilder> _settingDefinitionBuilderMock;

    public AiProviderBaseTests()
    {
        _capabilityFactoryMock = new Mock<IAiCapabilityFactory>();
        _settingDefinitionBuilderMock = new Mock<IAiSettingDefinitionBuilder>();

        _infrastructureMock = new Mock<IAiProviderInfrastructure>();
        _infrastructureMock.Setup(x => x.CapabilityFactory).Returns(_capabilityFactoryMock.Object);
        _infrastructureMock.Setup(x => x.SettingDefinitionBuilder).Returns(_settingDefinitionBuilderMock.Object);
    }

    #region Provider attribute

    [Fact]
    public void Provider_WithAttribute_HasCorrectIdAndName()
    {
        // Arrange & Act
        var provider = new TestProvider(_infrastructureMock.Object);

        // Assert
        provider.Id.ShouldBe("test-provider");
        provider.Name.ShouldBe("Test Provider");
    }

    [Fact]
    public void Provider_WithoutAttribute_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => new ProviderWithoutAttribute(_infrastructureMock.Object);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("missing the required AiProviderAttribute");
    }

    #endregion

    #region GetCapabilities

    [Fact]
    public void GetCapabilities_ReturnsRegisteredCapabilities()
    {
        // Arrange
        var chatCapability = new FakeChatCapability();
        var embeddingCapability = new FakeEmbeddingCapability();

        _capabilityFactoryMock
            .Setup(x => x.Create<IAiChatCapability>(It.IsAny<IAiProvider>()))
            .Returns(chatCapability);

        _capabilityFactoryMock
            .Setup(x => x.Create<IAiEmbeddingCapability>(It.IsAny<IAiProvider>()))
            .Returns(embeddingCapability);

        var provider = new ProviderWithMultipleCapabilities(_infrastructureMock.Object);

        // Act
        var capabilities = provider.GetCapabilities();

        // Assert
        capabilities.Count().ShouldBe(2);
        capabilities.ShouldContain(chatCapability);
        capabilities.ShouldContain(embeddingCapability);
    }

    [Fact]
    public void GetCapabilities_WithNoCapabilities_ReturnsEmptyCollection()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object);

        // Act
        var capabilities = provider.GetCapabilities();

        // Assert
        capabilities.ShouldBeEmpty();
    }

    #endregion

    #region GetCapability

    [Fact]
    public void GetCapability_WithExistingCapability_ReturnsCapability()
    {
        // Arrange
        var chatCapability = new FakeChatCapability();

        _capabilityFactoryMock
            .Setup(x => x.Create<IAiChatCapability>(It.IsAny<IAiProvider>()))
            .Returns(chatCapability);

        var provider = new ProviderWithChatCapability(_infrastructureMock.Object);

        // Act
        var result = provider.GetCapability<IAiChatCapability>();

        // Assert
        result.ShouldBe(chatCapability);
    }

    [Fact]
    public void GetCapability_WithMissingCapability_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object); // No capabilities

        // Act
        var act = () => provider.GetCapability<IAiChatCapability>();

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support the capability");
    }

    #endregion

    #region TryGetCapability

    [Fact]
    public void TryGetCapability_WithExistingCapability_ReturnsTrueAndCapability()
    {
        // Arrange
        var chatCapability = new FakeChatCapability();

        _capabilityFactoryMock
            .Setup(x => x.Create<IAiChatCapability>(It.IsAny<IAiProvider>()))
            .Returns(chatCapability);

        var provider = new ProviderWithChatCapability(_infrastructureMock.Object);

        // Act
        var result = provider.TryGetCapability<IAiChatCapability>(out var capability);

        // Assert
        result.ShouldBeTrue();
        capability.ShouldBe(chatCapability);
    }

    [Fact]
    public void TryGetCapability_WithMissingCapability_ReturnsFalseAndNull()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object);

        // Act
        var result = provider.TryGetCapability<IAiChatCapability>(out var capability);

        // Assert
        result.ShouldBeFalse();
        capability.ShouldBeNull();
    }

    #endregion

    #region HasCapability

    [Fact]
    public void HasCapability_WithExistingCapability_ReturnsTrue()
    {
        // Arrange
        var chatCapability = new FakeChatCapability();

        _capabilityFactoryMock
            .Setup(x => x.Create<IAiChatCapability>(It.IsAny<IAiProvider>()))
            .Returns(chatCapability);

        var provider = new ProviderWithChatCapability(_infrastructureMock.Object);

        // Act
        var result = provider.HasCapability<IAiChatCapability>();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasCapability_WithMissingCapability_ReturnsFalse()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object);

        // Act
        var result = provider.HasCapability<IAiChatCapability>();

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetSettingDefinitions

    [Fact]
    public void GetSettingDefinitions_BaseProvider_ReturnsEmptyList()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object);

        // Act
        var definitions = provider.GetSettingDefinitions();

        // Assert
        definitions.ShouldBeEmpty();
    }

    [Fact]
    public void GetSettingDefinitions_GenericProvider_BuildsFromSettingsType()
    {
        // Arrange
        var expectedDefinitions = new List<AiSettingDefinition>
        {
            new() { PropertyName = "ApiKey", Key = "api-key", Label = "API Key" }
        };

        _settingDefinitionBuilderMock
            .Setup(x => x.BuildForType<FakeProviderSettings>("typed-provider"))
            .Returns(expectedDefinitions);

        var provider = new TypedSettingsProvider(_infrastructureMock.Object);

        // Act
        var definitions = provider.GetSettingDefinitions();

        // Assert
        definitions.ShouldBe(expectedDefinitions);
        _settingDefinitionBuilderMock.Verify(
            x => x.BuildForType<FakeProviderSettings>("typed-provider"),
            Times.Once);
    }

    #endregion

    #region SettingsType

    [Fact]
    public void SettingsType_BaseProvider_ReturnsNull()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object);

        // Act & Assert
        provider.SettingsType.ShouldBeNull();
    }

    [Fact]
    public void SettingsType_GenericProvider_ReturnsSettingsType()
    {
        // Arrange
        var provider = new TypedSettingsProvider(_infrastructureMock.Object);

        // Act & Assert
        provider.SettingsType.ShouldBe(typeof(FakeProviderSettings));
    }

    #endregion

    #region Test providers

    [AiProvider("test-provider", "Test Provider")]
    private class TestProvider : AiProviderBase
    {
        public TestProvider(IAiProviderInfrastructure infrastructure)
            : base(infrastructure)
        { }
    }

    // Intentionally missing the AiProviderAttribute
    private class ProviderWithoutAttribute : AiProviderBase
    {
        public ProviderWithoutAttribute(IAiProviderInfrastructure infrastructure)
            : base(infrastructure)
        { }
    }

    [AiProvider("chat-provider", "Chat Provider")]
    private class ProviderWithChatCapability : AiProviderBase
    {
        public ProviderWithChatCapability(IAiProviderInfrastructure infrastructure)
            : base(infrastructure)
        {
            WithCapability<IAiChatCapability>();
        }
    }

    [AiProvider("multi-provider", "Multi Provider")]
    private class ProviderWithMultipleCapabilities : AiProviderBase
    {
        public ProviderWithMultipleCapabilities(IAiProviderInfrastructure infrastructure)
            : base(infrastructure)
        {
            WithCapability<IAiChatCapability>();
            WithCapability<IAiEmbeddingCapability>();
        }
    }

    [AiProvider("typed-provider", "Typed Provider")]
    private class TypedSettingsProvider : AiProviderBase<FakeProviderSettings>
    {
        public TypedSettingsProvider(IAiProviderInfrastructure infrastructure)
            : base(infrastructure)
        { }
    }

    #endregion
}
