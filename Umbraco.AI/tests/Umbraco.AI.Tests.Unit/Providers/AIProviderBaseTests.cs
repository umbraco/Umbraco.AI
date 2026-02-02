using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Providers;

public class AIProviderBaseTests
{
    private readonly Mock<IAIProviderInfrastructure> _infrastructureMock;
    private readonly Mock<IAICapabilityFactory> _capabilityFactoryMock;
    private readonly Mock<IAIEditableModelSchemaBuilder> _schemaBuilderMock;

    public AIProviderBaseTests()
    {
        _capabilityFactoryMock = new Mock<IAICapabilityFactory>();
        _schemaBuilderMock = new Mock<IAIEditableModelSchemaBuilder>();

        _infrastructureMock = new Mock<IAIProviderInfrastructure>();
        _infrastructureMock.Setup(x => x.CapabilityFactory).Returns(_capabilityFactoryMock.Object);
        _infrastructureMock.Setup(x => x.SchemaBuilder).Returns(_schemaBuilderMock.Object);
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
        exception.Message.ShouldContain("missing the required AIProviderAttribute");
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
            .Setup(x => x.Create<IAIChatCapability>(It.IsAny<IAIProvider>()))
            .Returns(chatCapability);

        _capabilityFactoryMock
            .Setup(x => x.Create<IAIEmbeddingCapability>(It.IsAny<IAIProvider>()))
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
            .Setup(x => x.Create<IAIChatCapability>(It.IsAny<IAIProvider>()))
            .Returns(chatCapability);

        var provider = new ProviderWithChatCapability(_infrastructureMock.Object);

        // Act
        var result = provider.GetCapability<IAIChatCapability>();

        // Assert
        result.ShouldBe(chatCapability);
    }

    [Fact]
    public void GetCapability_WithMissingCapability_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object); // No capabilities

        // Act
        var act = () => provider.GetCapability<IAIChatCapability>();

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
            .Setup(x => x.Create<IAIChatCapability>(It.IsAny<IAIProvider>()))
            .Returns(chatCapability);

        var provider = new ProviderWithChatCapability(_infrastructureMock.Object);

        // Act
        var result = provider.TryGetCapability<IAIChatCapability>(out var capability);

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
        var result = provider.TryGetCapability<IAIChatCapability>(out var capability);

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
            .Setup(x => x.Create<IAIChatCapability>(It.IsAny<IAIProvider>()))
            .Returns(chatCapability);

        var provider = new ProviderWithChatCapability(_infrastructureMock.Object);

        // Act
        var result = provider.HasCapability<IAIChatCapability>();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasCapability_WithMissingCapability_ReturnsFalse()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object);

        // Act
        var result = provider.HasCapability<IAIChatCapability>();

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetSettingsSchema

    [Fact]
    public void GetSettingsSchema_BaseProvider_ReturnsNull()
    {
        // Arrange
        var provider = new TestProvider(_infrastructureMock.Object);

        // Act
        var schema = provider.GetSettingsSchema();

        // Assert
        schema.ShouldBeNull();
    }

    [Fact]
    public void GetSettingsSchema_GenericProvider_BuildsFromSettingsType()
    {
        // Arrange
        var expectedSchema = new AIEditableModelSchema(
            typeof(FakeProviderSettings),
            new List<AIEditableModelField>
            {
                new() { PropertyName = "ApiKey", Key = "api-key", Label = "API Key" }
            });

        _schemaBuilderMock
            .Setup(x => x.BuildForType<FakeProviderSettings>("typed-provider"))
            .Returns(expectedSchema);

        var provider = new TypedSettingsProvider(_infrastructureMock.Object);

        // Act
        var schema = provider.GetSettingsSchema();

        // Assert
        schema.ShouldBe(expectedSchema);
        _schemaBuilderMock.Verify(
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

    [AIProvider("test-provider", "Test Provider")]
    private class TestProvider : AIProviderBase
    {
        public TestProvider(IAIProviderInfrastructure infrastructure)
            : base(infrastructure)
        { }
    }

    // Intentionally missing the AIProviderAttribute
    private class ProviderWithoutAttribute : AIProviderBase
    {
        public ProviderWithoutAttribute(IAIProviderInfrastructure infrastructure)
            : base(infrastructure)
        { }
    }

    [AIProvider("chat-provider", "Chat Provider")]
    private class ProviderWithChatCapability : AIProviderBase
    {
        public ProviderWithChatCapability(IAIProviderInfrastructure infrastructure)
            : base(infrastructure)
        {
            WithCapability<IAIChatCapability>();
        }
    }

    [AIProvider("multi-provider", "Multi Provider")]
    private class ProviderWithMultipleCapabilities : AIProviderBase
    {
        public ProviderWithMultipleCapabilities(IAIProviderInfrastructure infrastructure)
            : base(infrastructure)
        {
            WithCapability<IAIChatCapability>();
            WithCapability<IAIEmbeddingCapability>();
        }
    }

    [AIProvider("typed-provider", "Typed Provider")]
    private class TypedSettingsProvider : AIProviderBase<FakeProviderSettings>
    {
        public TypedSettingsProvider(IAIProviderInfrastructure infrastructure)
            : base(infrastructure)
        { }
    }

    #endregion
}
