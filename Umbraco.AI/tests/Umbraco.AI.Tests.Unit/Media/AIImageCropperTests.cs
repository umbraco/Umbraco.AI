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
        // Umbraco cropper coords for "left half, full height" on a 2000x1000 source:
        //   X1 = 0.0 (crop starts at left edge)
        //   Y1 = 0.0 (crop starts at top edge)
        //   X2 = 0.5 (crop right edge sits 0.5 from the right, i.e. halfway across)
        //   Y2 = 0.0 (crop bottom extends all the way to the bottom)
        // Resized to 400x400 target.
        var content = CreatePngContent(2000, 1000);
        var crops = new List<ImageCropperCrop>
        {
            new()
            {
                Alias = "content3Col",
                Width = 400,
                Height = 400,
                Coordinates = new ImageCropperCropCoordinates { X1 = 0m, Y1 = 0m, X2 = 0.5m, Y2 = 0m }
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
    public void ApplyCrop_WithOffsetFromEdgeSemantics_ExtractsRightHalf()
    {
        // Regression test for crop-coordinate semantics.
        //
        // Umbraco stores X2/Y2 as distances from the *right* and *bottom* edges,
        // not absolute positions. For a "right half" crop on a 2000-wide source
        // the stored coordinates are therefore X1=0.5 (start halfway across),
        // X2=0.0 (zero distance from the right edge, i.e. crop extends to the right).
        //
        // Treating X2 as an absolute end coordinate would compute the width as
        // (X2 - X1) * sourceWidth = -1000, clamping to a 1-pixel sliver. This
        // test asserts the crop rectangle is the full right half (1000px wide).
        //
        // No target dimensions are set so the output mirrors the natural crop
        // rectangle size — isolates coordinate interpretation from resizing.
        var content = CreatePngContent(2000, 1000);
        var crops = new List<ImageCropperCrop>
        {
            new()
            {
                Alias = "rightHalf",
                Width = 0,
                Height = 0,
                Coordinates = new ImageCropperCropCoordinates { X1 = 0.5m, Y1 = 0m, X2 = 0m, Y2 = 0m }
            }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "rightHalf", NullLogger.Instance);

        using var resultImage = Image.Load(result.Data);
        resultImage.Width.ShouldBe(1000);
        resultImage.Height.ShouldBe(1000);
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
        // 1000x1000 source, centre 500x500 crop. Umbraco offset-from-edge coords:
        // 0.25 from every edge leaves a centred 500x500 rectangle.
        var content = CreatePngContent(1000, 1000);
        var crops = new List<ImageCropperCrop>
        {
            new()
            {
                Alias = "centre",
                Width = 0,
                Height = 0,
                Coordinates = new ImageCropperCropCoordinates { X1 = 0.25m, Y1 = 0.25m, X2 = 0.25m, Y2 = 0.25m }
            }
        };

        var result = AIImageCropper.ApplyCrop(content, crops, "centre", NullLogger.Instance);

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
