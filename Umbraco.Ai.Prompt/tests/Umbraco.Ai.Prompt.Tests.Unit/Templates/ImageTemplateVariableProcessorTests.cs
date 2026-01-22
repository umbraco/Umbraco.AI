using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.Ai.Prompt.Core.Media;
using Umbraco.Ai.Prompt.Core.Templates.Processors;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.Ai.Prompt.Tests.Unit.Templates;

public class ImageTemplateVariableProcessorTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IContentService> _mockContentService;
    private readonly Mock<IAiUmbracoMediaResolver> _mockResolver;
    private readonly Mock<ILogger<ImageTemplateVariableProcessor>> _mockLogger;
    private readonly ImageTemplateVariableProcessor _processor;

    public ImageTemplateVariableProcessorTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockContentService = new Mock<IContentService>();
        _mockResolver = new Mock<IAiUmbracoMediaResolver>();
        _mockLogger = new Mock<ILogger<ImageTemplateVariableProcessor>>();
        _processor = new ImageTemplateVariableProcessor(
            _mockMediaService.Object,
            _mockContentService.Object,
            _mockResolver.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Prefix_ReturnsImage()
    {
        // Act
        var result = _processor.Prefix;

        // Assert
        result.ShouldBe("image");
    }

    [Fact]
    public async Task ProcessAsync_WithMediaEntity_FetchesFromMediaService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header bytes
        var context = CreateContext(entityId, "media");

        var mockMedia = CreateMockMedia("/media/12345/image.png", "Test Image");
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia);

        _mockResolver
            .Setup(r => r.ResolveAsync("/media/12345/image.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiMediaContent { Data = imageData, MediaType = "image/png" });

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBeOfType<DataContent>();
        var dataContent = (DataContent)result[0];
        dataContent.MediaType.ShouldBe("image/png");
        result[1].ShouldBeOfType<TextContent>();
        ((TextContent)result[1]).Text.ShouldBe(" [Image: Test Image]");
        _mockMediaService.Verify(s => s.GetById(entityId), Times.Once);
        _mockContentService.Verify(s => s.GetById(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WithContentEntity_FetchesFromContentService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG header
        var context = CreateContext(entityId, "document");

        var mockContent = CreateMockContent("/media/uploads/photo.jpg", "Test Content");
        _mockContentService.Setup(s => s.GetById(entityId)).Returns(mockContent);

        _mockResolver
            .Setup(r => r.ResolveAsync("/media/uploads/photo.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiMediaContent { Data = imageData, MediaType = "image/jpeg" });

        // Act
        var result = (await _processor.ProcessAsync("heroImage", context)).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBeOfType<DataContent>();
        result[1].ShouldBeOfType<TextContent>();
        ((TextContent)result[1]).Text.ShouldBe(" [Image: Test Content]");
        _mockContentService.Verify(s => s.GetById(entityId), Times.Once);
        _mockMediaService.Verify(s => s.GetById(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_MissingEntityId_ReturnsEmpty()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["entityType"] = "media"
            // Missing entityId
        };

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_MissingEntityType_ReturnsEmpty()
    {
        // Arrange
        var context = new Dictionary<string, object?>
        {
            ["entityId"] = Guid.NewGuid().ToString()
            // Missing entityType
        };

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_EntityNotFound_ReturnsEmpty()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var context = CreateContext(entityId, "media");

        _mockMediaService.Setup(s => s.GetById(entityId)).Returns((IMedia?)null);

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_PropertyNotFound_ReturnsEmpty()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var context = CreateContext(entityId, "media");

        var mockMedia = new Mock<IMedia>();
        mockMedia.Setup(m => m.GetValue("missingProperty", null, null, false)).Returns((object?)null);
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia.Object);

        // Act
        var result = (await _processor.ProcessAsync("missingProperty", context)).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_ResolverReturnsNull_ReturnsEmpty()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var context = CreateContext(entityId, "media");

        var mockMedia = CreateMockMedia("/media/corrupted/file.png", "Test");
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia);

        _mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiMediaContent?)null);

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WithStringEntityId_ParsesCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var context = new Dictionary<string, object?>
        {
            ["entityId"] = entityId.ToString(), // String instead of Guid
            ["entityType"] = "media"
        };

        var mockMedia = CreateMockMedia("/media/test/image.png", "Test Image");
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia);

        _mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiMediaContent { Data = new byte[] { 1, 2, 3 }, MediaType = "image/png" });

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.ShouldNotBeEmpty();
        _mockMediaService.Verify(s => s.GetById(entityId), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WithGuidEntityId_WorksDirectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var context = new Dictionary<string, object?>
        {
            ["entityId"] = entityId, // Guid directly
            ["entityType"] = "media"
        };

        var mockMedia = CreateMockMedia("/media/test/image.png", "Test Image");
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia);

        _mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiMediaContent { Data = new byte[] { 1, 2, 3 }, MediaType = "image/png" });

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("Media")]
    [InlineData("MEDIA")]
    [InlineData("media")]
    public async Task ProcessAsync_EntityTypeCaseInsensitive(string entityType)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var context = CreateContext(entityId, entityType);

        var mockMedia = CreateMockMedia("/media/test/image.png", "Test Image");
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia);

        _mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiMediaContent { Data = new byte[] { 1, 2, 3 }, MediaType = "image/png" });

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.ShouldNotBeEmpty();
        _mockMediaService.Verify(s => s.GetById(entityId), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext(Guid.NewGuid(), "media");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => (await _processor.ProcessAsync(null!, context)).ToList());
    }

    [Fact]
    public async Task ProcessAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => (await _processor.ProcessAsync("path", null!)).ToList());
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyEntityName_UsesFallbackReferenceName()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var context = CreateContext(entityId, "media");

        var mockMedia = CreateMockMedia("/media/test/image.png", ""); // Empty name
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia);

        _mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiMediaContent { Data = new byte[] { 1, 2, 3 }, MediaType = "image/png" });

        // Act
        var result = (await _processor.ProcessAsync("umbracoFile", context)).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[1].ShouldBeOfType<TextContent>();
        ((TextContent)result[1]).Text.ShouldBe(" [Image: image_umbracoFile]");
    }

    private static Dictionary<string, object?> CreateContext(Guid entityId, string entityType)
    {
        return new Dictionary<string, object?>
        {
            ["entityId"] = entityId.ToString(),
            ["entityType"] = entityType
        };
    }

    private static IMedia CreateMockMedia(string umbracoFilePath, string name)
    {
        var mockMedia = new Mock<IMedia>();
        mockMedia.Setup(m => m.GetValue("umbracoFile", null, null, false)).Returns(umbracoFilePath);
        mockMedia.Setup(m => m.Name).Returns(name);
        return mockMedia.Object;
    }

    private static IContent CreateMockContent(string propertyValue, string name)
    {
        var mockContent = new Mock<IContent>();
        mockContent.Setup(m => m.GetValue("heroImage", null, null, false)).Returns(propertyValue);
        mockContent.Setup(m => m.Name).Returns(name);
        return mockContent.Object;
    }
}
