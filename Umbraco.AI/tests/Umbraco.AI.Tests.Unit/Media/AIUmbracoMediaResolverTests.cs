using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Media;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Umbraco.AI.Tests.Unit.Media;

/// <summary>
/// Tests for <see cref="AIUmbracoMediaResolver.GetMediaType"/> — the cheap, no-file-I/O MIME
/// type lookup that <c>MediaEntityAdapter</c> uses to gate its (expensive) file-content-extraction
/// path off the media's real underlying file rather than its editable display name.
/// </summary>
public class AIUmbracoMediaResolverTests
{
    private readonly Mock<IMediaService> _mediaServiceMock = new();
    private static readonly IShortStringHelper ShortStringHelper = new DefaultShortStringHelper(new DefaultShortStringHelperConfig());

    private AIUmbracoMediaResolver CreateResolver()
    {
        // Uses the non-obsolete ctor overload: the obsolete 5-arg one resolves its
        // ICoreScopeProvider via StaticServiceProvider.Instance, which isn't initialized in a
        // unit test and throws. GetMediaType never touches the scope provider or file
        // system anyway, so a mocked Lazy<ICoreScopeProvider> is never invoked.
        var mediaFileManager = new MediaFileManager(
            Mock.Of<IFileSystem>(),
            Mock.Of<IMediaPathScheme>(),
            NullLogger<MediaFileManager>.Instance,
            ShortStringHelper,
            Mock.Of<IServiceProvider>(),
            new Lazy<ICoreScopeProvider>(() => Mock.Of<ICoreScopeProvider>()));

        var optionsMonitor = new Mock<IOptionsMonitor<AIMediaOptions>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(new AIMediaOptions());

        return new AIUmbracoMediaResolver(
            _mediaServiceMock.Object,
            mediaFileManager,
            optionsMonitor.Object,
            NullLogger<AIUmbracoMediaResolver>.Instance);
    }

    /// <summary>
    /// Builds a real (non-mocked) <see cref="IMedia"/> with an <c>umbracoFile</c> property set to
    /// the given value. <see cref="ContentBase.GetValue{T}(string, string?, string?, bool)"/> reads
    /// through the concrete <see cref="Property"/>/<see cref="PropertyType"/> machinery, which Moq
    /// cannot usefully stub, so a real (in-memory, DB-free) property graph is built instead.
    /// </summary>
    private static IMedia CreateMediaWithUmbracoFile(string? umbracoFile)
    {
        var mediaType = new MediaType(ShortStringHelper, -1);

        if (umbracoFile is null)
        {
            return new Umbraco.Cms.Core.Models.Media("test-media", -1, mediaType, new PropertyCollection());
        }

        var propertyType = new PropertyType(ShortStringHelper, "Umbraco.UploadField", ValueStorageType.Nvarchar, "umbracoFile");
        var property = new Property(propertyType);
        property.SetValue(umbracoFile);

        var properties = new PropertyCollection(new[] { property });
        return new Umbraco.Cms.Core.Models.Media("test-media", -1, mediaType, properties);
    }

    [Fact]
    public void GetMediaType_WithGuidResolvingToRealFile_ReturnsRealFileMediaType()
    {
        // Arrange — the media node's real umbracoFile is a .docx, regardless of what its display
        // name (entity.Name in the caller) might say.
        var mediaKey = Guid.NewGuid();
        var media = CreateMediaWithUmbracoFile("/media/1234/report.docx");
        _mediaServiceMock.Setup(m => m.GetById(mediaKey)).Returns(media);

        var resolver = CreateResolver();

        // Act
        var mediaType = resolver.GetMediaType(mediaKey);

        // Assert
        mediaType.ShouldBe("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    [Fact]
    public void GetMediaType_WhenMediaHasNoUmbracoFileProperty_ReturnsNull()
    {
        // Arrange
        var mediaKey = Guid.NewGuid();
        var media = CreateMediaWithUmbracoFile(null);
        _mediaServiceMock.Setup(m => m.GetById(mediaKey)).Returns(media);

        var resolver = CreateResolver();

        // Act
        var mediaType = resolver.GetMediaType(mediaKey);

        // Assert
        mediaType.ShouldBeNull();
    }

    [Fact]
    public void GetMediaType_WithNullValue_ReturnsNullWithoutThrowing()
    {
        // Arrange
        var resolver = CreateResolver();

        // Act
        var mediaType = resolver.GetMediaType(null);

        // Assert
        mediaType.ShouldBeNull();
    }

    [Fact]
    public void GetMediaType_WhenMediaNotFound_ReturnsNull()
    {
        // Arrange
        var mediaKey = Guid.NewGuid();
        _mediaServiceMock.Setup(m => m.GetById(mediaKey)).Returns((IMedia?)null);

        var resolver = CreateResolver();

        // Act
        var mediaType = resolver.GetMediaType(mediaKey);

        // Assert
        mediaType.ShouldBeNull();
    }
}
