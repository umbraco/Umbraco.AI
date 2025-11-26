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
        providers.Count.ShouldBe(2);
        providers.ShouldContain(provider1);
        providers.ShouldContain(provider2);
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
        providers.ShouldBeEmpty();
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
        result.ShouldNotBeNull();
        result.ShouldBe(provider);
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
        result.ShouldNotBeNull();
        result.ShouldBe(provider);
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
        result.ShouldNotBeNull();
        result.ShouldBe(provider);
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
        result.ShouldBeNull();
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
        result.ShouldBeNull();
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
        chatProviders.Count.ShouldBe(2);
        chatProviders.ShouldContain(chatProvider);
        chatProviders.ShouldContain(mixedProvider);
        chatProviders.ShouldNotContain(embeddingProvider);
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
        embeddingProviders.Count.ShouldBe(1);
        embeddingProviders.ShouldContain(embeddingProvider);
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
        chatProviders.ShouldBeEmpty();
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
        chatProviders.ShouldBeEmpty();
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
        result.ShouldNotBeNull();
        result.ShouldBe(chatCapability);
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
        result.ShouldBeNull();
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
        result.ShouldBeNull();
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
        result.ShouldBeNull();
    }

    #endregion
}
