using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Ai.Web.Api.Management.Embedding.Controllers;
using Umbraco.Ai.Web.Api.Management.Embedding.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Embedding;

public class GenerateEmbeddingControllerTests
{
    private readonly Mock<IAiEmbeddingService> _embeddingServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly GenerateEmbeddingController _controller;

    public GenerateEmbeddingControllerTests()
    {
        _embeddingServiceMock = new Mock<IAiEmbeddingService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new GenerateEmbeddingController(_embeddingServiceMock.Object, _mapperMock.Object);
    }

    #region GenerateEmbeddings

    [Fact]
    public async Task GenerateEmbeddings_WithValidRequest_ReturnsEmbeddings()
    {
        // Arrange
        var requestModel = new GenerateEmbeddingRequestModel
        {
            Values = new[] { "Hello world", "Test text" }
        };

        var embeddings = new GeneratedEmbeddings<Embedding<float>>(new[]
        {
            new Embedding<float>(new[] { 0.1f, 0.2f, 0.3f }),
            new Embedding<float>(new[] { 0.4f, 0.5f, 0.6f })
        });

        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        _mapperMock
            .Setup(x => x.Map<EmbeddingItemModel>(It.IsAny<Embedding<float>>()))
            .Returns((Embedding<float> e) => new EmbeddingItemModel { Vector = e.Vector.ToArray() });

        // Act
        var result = await _controller.GenerateEmbeddings(requestModel);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmbeddingResponseModel>();
        response.Embeddings.Count.ShouldBe(2);
        response.Embeddings[0].Index.ShouldBe(0);
        response.Embeddings[1].Index.ShouldBe(1);
    }

    [Fact]
    public async Task GenerateEmbeddings_WithProfileId_CallsServiceWithProfileId()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var requestModel = new GenerateEmbeddingRequestModel
        {
            ProfileId = profileId,
            Values = new[] { "Test text" }
        };

        var embeddings = new GeneratedEmbeddings<Embedding<float>>(new[]
        {
            new Embedding<float>(new[] { 0.1f, 0.2f, 0.3f })
        });

        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingsAsync(
                profileId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        _mapperMock
            .Setup(x => x.Map<EmbeddingItemModel>(It.IsAny<Embedding<float>>()))
            .Returns(new EmbeddingItemModel { Vector = new[] { 0.1f } });

        // Act
        await _controller.GenerateEmbeddings(requestModel);

        // Assert
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingsAsync(
            profileId,
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<EmbeddingGenerationOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddings_WithoutProfileId_CallsServiceWithDefaultProfile()
    {
        // Arrange
        var requestModel = new GenerateEmbeddingRequestModel
        {
            Values = new[] { "Test text" }
        };

        var embeddings = new GeneratedEmbeddings<Embedding<float>>(new[]
        {
            new Embedding<float>(new[] { 0.1f, 0.2f, 0.3f })
        });

        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        _mapperMock
            .Setup(x => x.Map<EmbeddingItemModel>(It.IsAny<Embedding<float>>()))
            .Returns(new EmbeddingItemModel { Vector = new[] { 0.1f } });

        // Act
        await _controller.GenerateEmbeddings(requestModel);

        // Assert - Should call the overload without profileId
        _embeddingServiceMock.Verify(x => x.GenerateEmbeddingsAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<EmbeddingGenerationOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddings_WithProfileNotFound_Returns404NotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var requestModel = new GenerateEmbeddingRequestModel
        {
            ProfileId = profileId,
            Values = new[] { "Test text" }
        };

        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingsAsync(
                profileId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Profile with ID '{profileId}' not found"));

        // Act
        var result = await _controller.GenerateEmbeddings(requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task GenerateEmbeddings_WithEmbeddingGenerationFailure_Returns400BadRequest()
    {
        // Arrange
        var requestModel = new GenerateEmbeddingRequestModel
        {
            Values = new[] { "Test text" }
        };

        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API rate limit exceeded"));

        // Act
        var result = await _controller.GenerateEmbeddings(requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Embedding generation failed");
        problemDetails.Detail.ShouldBe("API rate limit exceeded");
        problemDetails.Status.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GenerateEmbeddings_SetsCorrectIndexOnEachEmbedding()
    {
        // Arrange
        var requestModel = new GenerateEmbeddingRequestModel
        {
            Values = new[] { "First", "Second", "Third" }
        };

        var embeddings = new GeneratedEmbeddings<Embedding<float>>(new[]
        {
            new Embedding<float>(new[] { 0.1f }),
            new Embedding<float>(new[] { 0.2f }),
            new Embedding<float>(new[] { 0.3f })
        });

        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        _mapperMock
            .Setup(x => x.Map<EmbeddingItemModel>(It.IsAny<Embedding<float>>()))
            .Returns((Embedding<float> e) => new EmbeddingItemModel { Vector = e.Vector.ToArray() });

        // Act
        var result = await _controller.GenerateEmbeddings(requestModel);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<EmbeddingResponseModel>();
        response.Embeddings.Count.ShouldBe(3);
        response.Embeddings[0].Index.ShouldBe(0);
        response.Embeddings[1].Index.ShouldBe(1);
        response.Embeddings[2].Index.ShouldBe(2);
    }

    #endregion
}
