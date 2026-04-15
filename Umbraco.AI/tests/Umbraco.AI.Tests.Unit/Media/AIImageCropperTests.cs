using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Umbraco.AI.Core.Media;

namespace Umbraco.AI.Tests.Unit.Media;

public class AIImageCropperTests
{
    [Fact]
    public void ApplyCrop_WithMatchingCropAlias_ReturnsCroppedAndResizedImage()
    {
        // 2000x1000 red source, crop defines left half (0..0.5) at 400x400 target.
        var content = CreatePngContent(2000, 1000);
        var crops = new List<ImageCropperCrop>
        {
            new()
            {
                Alias = "content3Col",
                Width = 400,
                Height = 400,
                Coordinates = new ImageCropperCropCoordinates { X1 = 0m, Y1 = 0m, X2 = 0.5m, Y2 = 1m }
            }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "content3Col", NullLogger.Instance);

        result.ShouldNotBeSameAs(content);
        result.MediaType.ShouldBe("image/jpeg");

        using var resultImage = Image.Load(result.Data);
        resultImage.Width.ShouldBe(400);
        resultImage.Height.ShouldBe(400);
    }

    [Fact]
    public void ApplyCrop_WithUnknownCropAlias_ReturnsOriginal()
    {
        var content = CreatePngContent(100, 100);
        var crops = new List<ImageCropperCrop>
        {
            new() { Alias = "square", Coordinates = new ImageCropperCropCoordinates() }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "nonexistent", NullLogger.Instance);

        result.ShouldBeSameAs(content);
    }

    [Fact]
    public void ApplyCrop_WithCropMatchingAliasButNoCoordinates_ReturnsOriginal()
    {
        var content = CreatePngContent(100, 100);
        var crops = new List<ImageCropperCrop>
        {
            new() { Alias = "named", Coordinates = null }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "named", NullLogger.Instance);

        result.ShouldBeSameAs(content);
    }

    [Fact]
    public void ApplyCrop_WithAliasCaseInsensitive_FindsMatch()
    {
        var content = CreatePngContent(200, 200);
        var crops = new List<ImageCropperCrop>
        {
            new()
            {
                Alias = "Content3Col",
                Width = 100,
                Height = 100,
                Coordinates = new ImageCropperCropCoordinates { X1 = 0m, Y1 = 0m, X2 = 0.5m, Y2 = 0.5m }
            }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "content3col", NullLogger.Instance);

        result.ShouldNotBeSameAs(content);
    }

    [Fact]
    public void ApplyCrop_WithCorruptBytes_ReturnsOriginal()
    {
        var content = new AIMediaContent
        {
            Data = new byte[] { 0x00, 0x01, 0x02, 0x03 },
            MediaType = "image/png"
        };
        var crops = new List<ImageCropperCrop>
        {
            new()
            {
                Alias = "any",
                Coordinates = new ImageCropperCropCoordinates { X1 = 0m, Y1 = 0m, X2 = 1m, Y2 = 1m }
            }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "any", NullLogger.Instance);

        result.ShouldBeSameAs(content);
    }

    [Fact]
    public void ApplyCrop_WithoutTargetDimensions_CropsOnlyAtNaturalSize()
    {
        // 1000x1000 source, crop takes the full image but declares no target dims
        var content = CreatePngContent(1000, 1000);
        var crops = new List<ImageCropperCrop>
        {
            new()
            {
                Alias = "full",
                Width = 0,
                Height = 0,
                Coordinates = new ImageCropperCropCoordinates { X1 = 0.25m, Y1 = 0.25m, X2 = 0.75m, Y2 = 0.75m }
            }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "full", NullLogger.Instance);

        using var resultImage = Image.Load(result.Data);
        resultImage.Width.ShouldBe(500);
        resultImage.Height.ShouldBe(500);
    }

    private static AIMediaContent CreatePngContent(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return new AIMediaContent { Data = stream.ToArray(), MediaType = "image/png" };
    }
}
