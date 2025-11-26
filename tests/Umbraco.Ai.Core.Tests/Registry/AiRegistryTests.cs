using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;
using Umbraco.Ai.Tests.Common.Fakes;

namespace Umbraco.Ai.Core.Tests.Registry;

public class AiRegistryTests
{
    #region Providers property

    [Fact]
    public void Providers_ReturnsAllRegisteredProviders()
    {
        // Arrange
        var provider1 = new FakeAiProvider("provider-1", "Provider 1");
        var provider2 = new FakeAiProvider("provider-2", "Provider 2");

        var collection = new AiProviderCollection(() => new[] { provider1, provider2 });
        var registry = new AiRegistry(collection);

        // Act
        var providers = registry.Providers.ToList();

        // Assert
        providers.Should().HaveCount(2);
        providers.Should().Contain(provider1);
        providers.Should().Contain(provider2);
    }

    [Fact]
    public void Providers_WithEmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var collection = new AiProviderCollection(() => Enumerable.Empty<IAiProvider>());
        var registry = new AiRegistry(collection);

        // Act
        var providers = registry.Providers.ToList();

        // Assert
        providers.Should().BeEmpty();
    }

    #endregion

    #region GetProvider

    [Fact]
    public void GetProvider_WithExistingId_ReturnsProvider()
    {
        // Arrange
        var provider = new FakeAiProvider("openai", "OpenAI");
        var collection = new AiProviderCollection(() => new[] { provider });
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetProvider("openai");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(provider);
    }

    [Fact]
    public void GetProvider_WithCaseInsensitiveMatch_ReturnsProvider()
    {
        // Arrange
        var provider = new FakeAiProvider("OpenAI", "OpenAI");
        var collection = new AiProviderCollection(() => new[] { provider });
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetProvider("openai");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(provider);
    }

    [Fact]
    public void GetProvider_WithUpperCaseInput_ReturnsProvider()
    {
        // Arrange
        var provider = new FakeAiProvider("openai", "OpenAI");
        var collection = new AiProviderCollection(() => new[] { provider });
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetProvider("OPENAI");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(provider);
    }

    [Fact]
    public void GetProvider_WithUnknownId_ReturnsNull()
    {
        // Arrange
        var provider = new FakeAiProvider("openai", "OpenAI");
        var collection = new AiProviderCollection(() => new[] { provider });
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetProvider("unknown-provider");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetProvider_WithEmptyCollection_ReturnsNull()
    {
        // Arrange
        var collection = new AiProviderCollection(() => Enumerable.Empty<IAiProvider>());
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetProvider("any-provider");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetProvidersWithCapability

    [Fact]
    public void GetProvidersWithCapability_WithMatchingProviders_ReturnsFilteredList()
    {
        // Arrange
        var chatProvider = new FakeAiProvider("chat-provider", "Chat Provider")
            .WithChatCapability();
        var embeddingProvider = new FakeAiProvider("embedding-provider", "Embedding Provider")
            .WithEmbeddingCapability();
        var mixedProvider = new FakeAiProvider("mixed-provider", "Mixed Provider")
            .WithChatCapability()
            .WithEmbeddingCapability();

        var collection = new AiProviderCollection(() => new[]
        {
            chatProvider,
            embeddingProvider,
            mixedProvider
        });
        var registry = new AiRegistry(collection);

        // Act
        var chatProviders = registry.GetProvidersWithCapability<IAiChatCapability>().ToList();

        // Assert
        chatProviders.Should().HaveCount(2);
        chatProviders.Should().Contain(chatProvider);
        chatProviders.Should().Contain(mixedProvider);
        chatProviders.Should().NotContain(embeddingProvider);
    }

    [Fact]
    public void GetProvidersWithCapability_ForEmbedding_ReturnsEmbeddingProviders()
    {
        // Arrange
        var chatProvider = new FakeAiProvider("chat-provider", "Chat Provider")
            .WithChatCapability();
        var embeddingProvider = new FakeAiProvider("embedding-provider", "Embedding Provider")
            .WithEmbeddingCapability();

        var collection = new AiProviderCollection(() => new[]
        {
            chatProvider,
            embeddingProvider
        });
        var registry = new AiRegistry(collection);

        // Act
        var embeddingProviders = registry.GetProvidersWithCapability<IAiEmbeddingCapability>().ToList();

        // Assert
        embeddingProviders.Should().HaveCount(1);
        embeddingProviders.Should().Contain(embeddingProvider);
    }

    [Fact]
    public void GetProvidersWithCapability_WithNoMatchingProviders_ReturnsEmpty()
    {
        // Arrange
        var embeddingProvider = new FakeAiProvider("embedding-provider", "Embedding Provider")
            .WithEmbeddingCapability();

        var collection = new AiProviderCollection(() => new[] { embeddingProvider });
        var registry = new AiRegistry(collection);

        // Act
        var chatProviders = registry.GetProvidersWithCapability<IAiChatCapability>().ToList();

        // Assert
        chatProviders.Should().BeEmpty();
    }

    [Fact]
    public void GetProvidersWithCapability_WithEmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var collection = new AiProviderCollection(() => Enumerable.Empty<IAiProvider>());
        var registry = new AiRegistry(collection);

        // Act
        var chatProviders = registry.GetProvidersWithCapability<IAiChatCapability>().ToList();

        // Assert
        chatProviders.Should().BeEmpty();
    }

    #endregion

    #region GetCapability

    [Fact]
    public void GetCapability_WithProviderHavingCapability_ReturnsCapability()
    {
        // Arrange
        var chatCapability = new FakeChatCapability();
        var provider = new FakeAiProvider("openai", "OpenAI")
            .WithChatCapability(chatCapability);

        var collection = new AiProviderCollection(() => new[] { provider });
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetCapability<IAiChatCapability>("openai");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(chatCapability);
    }

    [Fact]
    public void GetCapability_WithProviderLackingCapability_ReturnsNull()
    {
        // Arrange
        var provider = new FakeAiProvider("embedding-only", "Embedding Only")
            .WithEmbeddingCapability();

        var collection = new AiProviderCollection(() => new[] { provider });
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetCapability<IAiChatCapability>("embedding-only");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCapability_WithUnknownProvider_ReturnsNull()
    {
        // Arrange
        var provider = new FakeAiProvider("openai", "OpenAI")
            .WithChatCapability();

        var collection = new AiProviderCollection(() => new[] { provider });
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetCapability<IAiChatCapability>("unknown-provider");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCapability_WithEmptyCollection_ReturnsNull()
    {
        // Arrange
        var collection = new AiProviderCollection(() => Enumerable.Empty<IAiProvider>());
        var registry = new AiRegistry(collection);

        // Act
        var result = registry.GetCapability<IAiChatCapability>("any-provider");

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
