using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
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

        _service = new AISemanticSearchService(
            sources,
            _embeddingServiceMock.Object,
            _repositoryMock.Object,
            _profileServiceMock.Object,
            options,
            Mock.Of<ILogger<AISemanticSearchService>>());
    }

    [Fact]
    public async Task SearchAsync_ReturnsSortedBySimilarity()
    {
        // Arrange
        float[] queryVector = [1f, 0f, 0f];
        float[] highSimilarityVector = [0.9f, 0.1f, 0f]; // High similarity
        float[] lowSimilarityVector = [0.5f, 0.5f, 0.5f]; // Lower similarity

        SetupQueryEmbedding(queryVector);

        var embeddings = new List<AIEmbedding>
        {
            CreateEmbedding(Guid.NewGuid(), "Low Match", "content", lowSimilarityVector),
            CreateEmbedding(Guid.NewGuid(), "High Match", "content", highSimilarityVector),
        };

        _repositoryMock.Setup(r => r.GetByFilterAsync(It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        // Act
        var results = await _service.SearchAsync("test query");

        // Assert
        results.Count.ShouldBe(2);
        results[0].Name.ShouldBe("High Match");
        results[1].Name.ShouldBe("Low Match");
        results[0].SimilarityScore.ShouldBeGreaterThan(results[1].SimilarityScore);
    }

    [Fact]
    public async Task SearchAsync_WithTypeFilter_PassesFilterToRepository()
    {
        // Arrange
        float[] queryVector = [1f, 0f];
        float[] matchVector = [0.9f, 0.1f];

        SetupQueryEmbedding(queryVector);

        var filteredEmbeddings = new List<AIEmbedding>
        {
            CreateEmbedding(Guid.NewGuid(), "Content Item", "content", matchVector),
        };

        _repositoryMock.Setup(r => r.GetByFilterAsync("content", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(filteredEmbeddings);

        var options = new SemanticSearchQueryOptions(TypeFilter: "content");

        // Act
        var results = await _service.SearchAsync("test", options);

        // Assert
        results.Count.ShouldBe(1);
        results[0].EntityType.ShouldBe("content");
        _repositoryMock.Verify(r => r.GetByFilterAsync("content", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_BelowMinimumSimilarity_ExcludesResults()
    {
        // Arrange
        float[] queryVector = [1f, 0f];
        float[] orthogonalVector = [0f, 1f]; // Cosine similarity ~0

        SetupQueryEmbedding(queryVector);

        var embeddings = new List<AIEmbedding>
        {
            CreateEmbedding(Guid.NewGuid(), "Unrelated", "content", orthogonalVector),
        };

        _repositoryMock.Setup(r => r.GetByFilterAsync(It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        var options = new SemanticSearchQueryOptions(MinimumSimilarity: 0.5f);

        // Act
        var results = await _service.SearchAsync("test", options);

        // Assert
        results.Count.ShouldBe(0);
    }

    [Fact]
    public async Task SearchAsync_NoEmbeddings_ReturnsEmpty()
    {
        // Arrange
        float[] queryVector = [1f, 0f];
        SetupQueryEmbedding(queryVector);

        _repositoryMock.Setup(r => r.GetByFilterAsync(It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIEmbedding>());

        // Act
        var results = await _service.SearchAsync("test");

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_RespectsMaxResults()
    {
        // Arrange
        float[] queryVector = [1f, 0f];
        SetupQueryEmbedding(queryVector);

        var embeddings = Enumerable.Range(0, 20)
            .Select(i => CreateEmbedding(Guid.NewGuid(), $"Item {i}", "content", [0.9f, 0.1f]))
            .ToList();

        _repositoryMock.Setup(r => r.GetByFilterAsync(It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        var options = new SemanticSearchQueryOptions(MaxResults: 5);

        // Act
        var results = await _service.SearchAsync("test", options);

        // Assert
        results.Count.ShouldBe(5);
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
        EntityTypeAlias = "article",
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
