using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.Ai.Prompt.Core.Media;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Xunit;

namespace Umbraco.Ai.Prompt.Tests.Unit.Media;

public class AiMediaImageResolverTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILogger<AiMediaImageResolver>> _mockLogger;
    private readonly AiMediaImageResolver _resolver;

    public AiMediaImageResolverTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockFileSystem = new Mock<IFileSystem>();

        // Create a real MediaFileManager with our mocked file system
        // MediaFileManager is sealed so we can't mock it, but we can create a real instance
#pragma warning disable CS0618 // Obsolete constructor - needed for testing
        _mediaFileManager = new MediaFileManager(
            _mockFileSystem.Object,
            Mock.Of<IMediaPathScheme>(),
            NullLogger<MediaFileManager>.Instance,
            Mock.Of<IShortStringHelper>(),
            Mock.Of<IServiceProvider>());
#pragma warning restore CS0618
        _mockLogger = new Mock<ILogger<AiMediaImageResolver>>();

        _resolver = new AiMediaImageResolver(
            _mockMediaService.Object,
            _mediaFileManager,
            _mockLogger.Object);
    }

    [Fact]
    public void Resolve_WithNullValue_ReturnsNull()
    {
        // Act
        var result = _resolver.Resolve(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WithGuid_FetchesFromMediaService()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var mockMedia = CreateMockMedia("/media/12345/image.png");

        _mockMediaService.Setup(s => s.GetById(mediaId)).Returns(mockMedia);
        SetupFileSystem("/media/12345/image.png", new byte[] { 1, 2, 3 });

        // Act
        var result = _resolver.Resolve(mediaId);

        // Assert
        result.ShouldNotBeNull();
        result.MediaType.ShouldBe("image/png");
        _mockMediaService.Verify(s => s.GetById(mediaId), Times.Once);
    }

    [Fact]
    public void Resolve_WithGuidString_ParsesAndFetchesFromMediaService()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var mockMedia = CreateMockMedia("/media/12345/image.jpg");

        _mockMediaService.Setup(s => s.GetById(mediaId)).Returns(mockMedia);
        SetupFileSystem("/media/12345/image.jpg", new byte[] { 1, 2, 3 });

        // Act
        var result = _resolver.Resolve(mediaId.ToString());

        // Assert
        result.ShouldNotBeNull();
        result.MediaType.ShouldBe("image/jpeg");
    }

    [Fact]
    public void Resolve_WithMediaPickerJson_ExtractsMediaKey()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new { mediaKey = mediaId.ToString() });
        var mockMedia = CreateMockMedia("/media/12345/image.png");

        _mockMediaService.Setup(s => s.GetById(mediaId)).Returns(mockMedia);
        SetupFileSystem("/media/12345/image.png", new byte[] { 1, 2, 3 });

        // Act
        var result = _resolver.Resolve(json);

        // Assert
        result.ShouldNotBeNull();
        _mockMediaService.Verify(s => s.GetById(mediaId), Times.Once);
    }

    [Fact]
    public void Resolve_WithMediaPickerArrayJson_ExtractsFirstMediaKey()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new[] { new { mediaKey = mediaId.ToString() } });
        var mockMedia = CreateMockMedia("/media/12345/image.gif");

        _mockMediaService.Setup(s => s.GetById(mediaId)).Returns(mockMedia);
        SetupFileSystem("/media/12345/image.gif", new byte[] { 1, 2, 3 });

        // Act
        var result = _resolver.Resolve(json);

        // Assert
        result.ShouldNotBeNull();
        result.MediaType.ShouldBe("image/gif");
    }

    [Fact]
    public void Resolve_WithImageCropperJson_ExtractsSrcPath()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new { src = "/media/12345/cropped.webp" });

        SetupFileSystem("/media/12345/cropped.webp", new byte[] { 1, 2, 3 });

        // Act
        var result = _resolver.Resolve(json);

        // Assert
        result.ShouldNotBeNull();
        result.MediaType.ShouldBe("image/webp");
    }

    [Fact]
    public void Resolve_WithFilePath_ReadsDirectlyFromStorage()
    {
        // Arrange
        var filePath = "/media/uploads/photo.bmp";

        SetupFileSystem(filePath, new byte[] { 0x42, 0x4D }); // BMP header

        // Act
        var result = _resolver.Resolve(filePath);

        // Assert
        result.ShouldNotBeNull();
        result.MediaType.ShouldBe("image/bmp");
        result.Data.ShouldBe(new byte[] { 0x42, 0x4D });
    }

    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".bmp", "image/bmp")]
    public void Resolve_WithSupportedExtensions_ReturnsCorrectMediaType(string extension, string expectedMediaType)
    {
        // Arrange
        var filePath = $"/media/test/image{extension}";
        SetupFileSystem(filePath, new byte[] { 1, 2, 3 });

        // Act
        var result = _resolver.Resolve(filePath);

        // Assert
        result.ShouldNotBeNull();
        result.MediaType.ShouldBe(expectedMediaType);
    }

    [Fact]
    public void Resolve_WithUnsupportedExtension_ReturnsNull()
    {
        // Arrange
        var filePath = "/media/test/document.pdf";

        // Act
        var result = _resolver.Resolve(filePath);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var filePath = "/media/missing/image.png";
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        var result = _resolver.Resolve(filePath);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WithMediaNotFound_ReturnsNull()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        _mockMediaService.Setup(s => s.GetById(mediaId)).Returns((IMedia?)null);

        // Act
        var result = _resolver.Resolve(mediaId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WithJsonElement_ExtractsCorrectly()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { mediaKey = mediaId.ToString() }));
        var jsonElement = doc.RootElement;
        var mockMedia = CreateMockMedia("/media/12345/image.png");

        _mockMediaService.Setup(s => s.GetById(mediaId)).Returns(mockMedia);
        SetupFileSystem("/media/12345/image.png", new byte[] { 1, 2, 3 });

        // Act
        var result = _resolver.Resolve(jsonElement);

        // Assert
        result.ShouldNotBeNull();
    }

    private IMedia CreateMockMedia(string umbracoFilePath)
    {
        var mockMedia = new Mock<IMedia>();
        mockMedia.Setup(m => m.GetValue<string>("umbracoFile", null, null, false)).Returns(umbracoFilePath);
        return mockMedia.Object;
    }

    private void SetupFileSystem(string fullPath, byte[] content)
    {
        // Normalize the path as AiMediaImageResolver does
        var relativePath = fullPath;
        if (relativePath.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath[7..];
        }
        else if (relativePath.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath[6..];
        }

        _mockFileSystem.Setup(fs => fs.FileExists(relativePath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.OpenFile(relativePath)).Returns(new MemoryStream(content));
    }
}
