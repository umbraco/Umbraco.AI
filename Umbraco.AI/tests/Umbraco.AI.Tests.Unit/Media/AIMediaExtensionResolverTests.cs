using Umbraco.AI.Core.Media;

namespace Umbraco.AI.Tests.Unit.Media;

public class AIMediaExtensionResolverTests
{
    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".bmp", "image/bmp")]
    [InlineData(".mp3", "audio/mpeg")]
    [InlineData(".wav", "audio/wav")]
    [InlineData(".m4a", "audio/mp4")]
    [InlineData(".mp4", "audio/mp4")]
    [InlineData(".ogg", "audio/ogg")]
    [InlineData(".oga", "audio/ogg")]
    [InlineData(".webm", "audio/webm")]
    [InlineData(".flac", "audio/flac")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".md", "text/markdown")]
    [InlineData(".csv", "text/csv")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    public void TryGetMediaType_WithSupportedExtension_ReturnsExpectedMediaType(string extension, string expectedMediaType)
    {
        var result = AIMediaExtensionResolver.TryGetMediaType(extension, out var mediaType);

        result.ShouldBeTrue();
        mediaType.ShouldBe(expectedMediaType);
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".pdf")]
    [InlineData(".zip")]
    [InlineData("")]
    public void TryGetMediaType_WithUnsupportedExtension_ReturnsFalse(string extension)
    {
        var result = AIMediaExtensionResolver.TryGetMediaType(extension, out var mediaType);

        result.ShouldBeFalse();
        mediaType.ShouldBeNull();
    }

    [Theory]
    [InlineData(".JPG", "image/jpeg")]
    [InlineData(".CSV", "text/csv")]
    public void TryGetMediaType_IsCaseInsensitive(string extension, string expectedMediaType)
    {
        var result = AIMediaExtensionResolver.TryGetMediaType(extension, out var mediaType);

        result.ShouldBeTrue();
        mediaType.ShouldBe(expectedMediaType);
    }
}
