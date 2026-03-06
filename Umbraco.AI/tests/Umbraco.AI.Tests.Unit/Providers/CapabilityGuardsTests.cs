using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Providers;

public class CapabilityGuardsTests
{
    #region GetModelsAsync with unresolved settings

    [Fact]
    public async Task GetModelsAsync_WithJsonElementSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var capability = new TestChatCapability();
        var jsonElement = JsonDocument.Parse("{}").RootElement;

        // Act
        var act = () => ((IAICapability)capability).GetModelsAsync(jsonElement);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Settings must be resolved");
        exception.Message.ShouldContain("GetModelsAsync");
        exception.Message.ShouldContain("IAIConfiguredProvider");
    }

    [Fact]
    public async Task GetModelsAsync_WithResolvedSettings_Succeeds()
    {
        // Arrange
        var capability = new TestChatCapability();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };

        // Act
        var result = await ((IAICapability)capability).GetModelsAsync(settings);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetModelsAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var capability = new TestChatCapability();

        // Act
        var act = () => ((IAICapability)capability).GetModelsAsync(null);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region CreateClientAsync with unresolved settings

    [Fact]
    public async Task CreateClientAsync_WithJsonElementSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var capability = new TestChatCapability();
        var jsonElement = JsonDocument.Parse("{}").RootElement;

        // Act
        var act = () => ((IAIChatCapability)capability).CreateClientAsync(jsonElement);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Settings must be resolved");
        exception.Message.ShouldContain("CreateClient");
        exception.Message.ShouldContain("IAIConfiguredProvider");
    }

    [Fact]
    public async Task CreateClientAsync_WithResolvedSettings_Succeeds()
    {
        // Arrange
        var capability = new TestChatCapability();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };

        // Act
        var result = await ((IAIChatCapability)capability).CreateClientAsync(settings);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateClientAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var capability = new TestChatCapability();

        // Act
        var act = () => ((IAIChatCapability)capability).CreateClientAsync(null);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region CreateGeneratorAsync with unresolved settings

    [Fact]
    public async Task CreateGeneratorAsync_WithJsonElementSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var capability = new TestEmbeddingCapability();
        var jsonElement = JsonDocument.Parse("{}").RootElement;

        // Act
        var act = () => ((IAIEmbeddingCapability)capability).CreateGeneratorAsync(jsonElement);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("Settings must be resolved");
        exception.Message.ShouldContain("CreateGenerator");
        exception.Message.ShouldContain("IAIConfiguredProvider");
    }

    [Fact]
    public async Task CreateGeneratorAsync_WithResolvedSettings_Succeeds()
    {
        // Arrange
        var capability = new TestEmbeddingCapability();
        var settings = new FakeProviderSettings { ApiKey = "test-key" };

        // Act
        var result = await ((IAIEmbeddingCapability)capability).CreateGeneratorAsync(settings);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateGeneratorAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var capability = new TestEmbeddingCapability();

        // Act
        var act = () => ((IAIEmbeddingCapability)capability).CreateGeneratorAsync(null);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region Test capabilities

    private class TestChatCapability : AIChatCapabilityBase<FakeProviderSettings>
    {
        private static readonly FakeAIProvider FakeProvider = new("test", "Test");

        public TestChatCapability() : base(FakeProvider)
        {
        }

        protected override IChatClient CreateClient(FakeProviderSettings settings, string? modelId)
            => new FakeChatClient();

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>(new List<AIModelDescriptor>
            {
                new(new AIModelRef("test", "model-1"), "Model 1")
            });
    }

    private class TestEmbeddingCapability : AIEmbeddingCapabilityBase<FakeProviderSettings>
    {
        private static readonly FakeAIProvider FakeProvider = new("test", "Test");

        public TestEmbeddingCapability() : base(FakeProvider)
        {
        }

        protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(FakeProviderSettings settings, string? modelId)
            => new FakeEmbeddingGenerator();

        protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
            FakeProviderSettings settings,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AIModelDescriptor>>(new List<AIModelDescriptor>
            {
                new(new AIModelRef("test", "embed-1"), "Embedding Model 1")
            });
    }

    #endregion
}
