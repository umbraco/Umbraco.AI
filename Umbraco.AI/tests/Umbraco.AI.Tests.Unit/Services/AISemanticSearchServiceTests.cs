using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SemanticSearch;
using Umbraco.AI.Tests.Common.Builders;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Services;

public class AISemanticSearchServiceTests
{
    private readonly Mock<IAIEmbeddingService> _embeddingServiceMock;
    private readonly Mock<IAIEmbeddingsRepository> _repositoryMock;
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly AISemanticSearchService _service;

    public AISemanticSearchServiceTests()
    {
        _embeddingServiceMock = new Mock<IAIEmbeddingService>();
        _repositoryMock = new Mock<IAIEmbeddingsRepository>();
        _profileServiceMock = new Mock<IAIProfileService>();

        var options = Options.Create(new AISemanticSearchOptions
        {
            Enabled = true,
            MaxTextLength = 8000,
            BatchSize = 50
        });

        var profile = CreateProfile(Guid.NewGuid(), "test-model");
        _profileServiceMock
            .Setup(p => p.GetDefaultProfileAsync(AICapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var sources = new List<ISemanticIndexSource>();

        var scopeProviderMock = new Mock<IAIRuntimeContextScopeProvider>();
        var scopeMock = new Mock<IAIRuntimeContextScope>();
        scopeMock.Setup(s => s.Context).Returns(new AIRuntimeContext([]));
        scopeProviderMock.Setup(s => s.CreateScope()).Returns(scopeMock.Object);

        _service = new AISemanticSearchService(
            sources,
            _embeddingServiceMock.Object,
            _repositoryMock.Object,
            _profileServiceMock.Object,
            scopeProviderMock.Object,
            options,
            Mock.Of<ILogger<AISemanticSearchService>>());
    }

    [Fact]
    public async Task SearchAsync_DelegatesToRepositoryAndMapsResults()
    {
        // Arrange
        float[] queryVector = [1f, 0f, 0f];
        SetupQueryEmbedding(queryVector);

        var highMatch = CreateEmbedding(Guid.NewGuid(), "High Match", "content", [0.9f, 0.1f, 0f]);
        var lowMatch = CreateEmbedding(Guid.NewGuid(), "Low Match", "content", [0.5f, 0.5f, 0.5f]);

        var repositoryResults = new List<EmbeddingSimilarityResult>
        {
            new(highMatch, 0.95f),
            new(lowMatch, 0.72f),
        };

        _repositoryMock.Setup(r => r.SearchByVectorAsync(
                It.IsAny<float[]>(), null, null, 0.5f, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryResults);

        // Act
        var results = await _service.SearchAsync("test query");

        // Assert
        results.Count.ShouldBe(2);
        results[0].Name.ShouldBe("High Match");
        results[0].SimilarityScore.ShouldBe(0.95f);
        results[1].Name.ShouldBe("Low Match");
        results[1].SimilarityScore.ShouldBe(0.72f);
    }

    [Fact]
    public async Task SearchAsync_PassesOptionsToRepository()
    {
        // Arrange
        float[] queryVector = [1f, 0f];
        SetupQueryEmbedding(queryVector);

        var contentItem = CreateEmbedding(Guid.NewGuid(), "Content Item", "content", [0.9f, 0.1f]);
        var repositoryResults = new List<EmbeddingSimilarityResult> { new(contentItem, 0.9f) };

        string[]? capturedAliases = null;
        _repositoryMock.Setup(r => r.SearchByVectorAsync(
                It.IsAny<float[]>(), "content", It.IsAny<string[]?>(), 0.7f, 5, It.IsAny<CancellationToken>()))
            .Callback<float[], string?, string[]?, float, int, CancellationToken>(
                (_, _, aliases, _, _, _) => capturedAliases = aliases)
            .ReturnsAsync(repositoryResults);

        var aliases = new[] { "article", "blogPost" };
        var options = new SemanticSearchQueryOptions(
            TypeFilter: "content",
            EntitySubTypes: aliases,
            MaxResults: 5,
            MinimumSimilarity: 0.7f);

        // Act
        var results = await _service.SearchAsync("test", options);

        // Assert
        results.Count.ShouldBe(1);
        _repositoryMock.Verify(r => r.SearchByVectorAsync(
            It.IsAny<float[]>(), "content", aliases, 0.7f, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_NoResults_ReturnsEmpty()
    {
        // Arrange
        float[] queryVector = [1f, 0f];
        SetupQueryEmbedding(queryVector);

        _repositoryMock.Setup(r => r.SearchByVectorAsync(
                It.IsAny<float[]>(), It.IsAny<string?>(), It.IsAny<string[]?>(),
                It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmbeddingSimilarityResult>());

        // Act
        var results = await _service.SearchAsync("test");

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_NoEmbeddingProfile_ReturnsEmpty()
    {
        // Arrange
        _profileServiceMock
            .Setup(p => p.GetDefaultProfileAsync(AICapability.Embedding, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIProfileNotFoundException(AICapability.Embedding, "No default profile"));

        // Act
        var results = await _service.SearchAsync("test query");

        // Assert
        results.ShouldBeEmpty();
        _repositoryMock.Verify(
            r => r.SearchByVectorAsync(
                It.IsAny<float[]>(), It.IsAny<string?>(), It.IsAny<string[]?>(),
                It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetIndexStatusAsync_ReturnsCount()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var status = await _service.GetIndexStatusAsync();

        // Assert
        status.TotalIndexed.ShouldBe(42);
        status.ProfileId.ShouldNotBeNull();
    }

    private void SetupQueryEmbedding(float[] vector)
    {
        _embeddingServiceMock
            .Setup(e => e.GenerateEmbeddingAsync(
                It.IsAny<string>(),
                It.IsAny<EmbeddingGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Embedding<float>(vector));
    }

    private static AIEmbedding CreateEmbedding(Guid key, string name, string contentType, float[] vector) => new()
    {
        Id = Guid.NewGuid(),
        EntityKey = key,
        EntityType = contentType,
        EntitySubType = "article",
        Name = name,
        TextContent = $"Text for {name}",
        Vector = VectorMath.SerializeVector(vector),
        Dimensions = vector.Length,
        ProfileId = Guid.NewGuid(),
        ModelId = "test-model",
        DateIndexed = DateTime.UtcNow,
        EntityDateModified = DateTime.UtcNow
    };

    private static AIProfile CreateProfile(Guid id, string modelId)
    {
        return new AIProfileBuilder()
            .WithId(id)
            .WithAlias("test-embedding")
            .WithName("Test Embedding")
            .WithCapability(AICapability.Embedding)
            .WithModel("test-provider", modelId)
            .Build();
    }
}
