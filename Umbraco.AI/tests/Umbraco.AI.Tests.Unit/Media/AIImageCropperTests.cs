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
    public void ApplyCrop_WhenPngHasAlpha_PreservesAsPngAndKeepsTransparency()
    {
        // Rgba32 with transparent pixels — cropping must not flatten transparency
        // to an opaque JPEG background.
        var content = CreatePngWithAlphaContent(2000, 2000);
        var crops = new List<ImageCropperCrop>
        {
            new()
            {
                Alias = "half",
                Width = 400,
                Height = 400,
                Coordinates = new ImageCropperCropCoordinates { X1 = 0m, Y1 = 0m, X2 = 0.5m, Y2 = 0.5m }
            }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "half", NullLogger.Instance);

        result.MediaType.ShouldBe("image/png");

        using var resultImage = Image.Load<Rgba32>(result.Data);
        resultImage.Width.ShouldBe(400);
        resultImage.Height.ShouldBe(400);
        // Source fill was A=128; a centre pixel should stay non-opaque after crop+resize.
        resultImage[200, 200].A.ShouldBeLessThan((byte)255);
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

    // Opaque PNG (Rgb24) — round-trips through Image.Load as no-alpha, so the
    // cropper is free to re-encode as JPEG.
    private static AIMediaContent CreatePngContent(int width, int height)
    {
        using var image = new Image<Rgb24>(width, height);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return new AIMediaContent { Data = stream.ToArray(), MediaType = "image/png" };
    }

    // PNG with alpha (Rgba32). A uniform fully-transparent image gets optimised
    // by the encoder into a palette/grayscale form without alpha; fill with a
    // semi-transparent colour so the alpha channel survives the round-trip.
    // Top-left pixel is made fully transparent so assertions have a concrete
    // reference point after cropping from the (0,0) origin.
    private static AIMediaContent CreatePngWithAlphaContent(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgba32(255, 0, 0, 128);
                }
            }
        });
        image[0, 0] = new Rgba32(0, 0, 0, 0);

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return new AIMediaContent { Data = stream.ToArray(), MediaType = "image/png" };
    }
}
