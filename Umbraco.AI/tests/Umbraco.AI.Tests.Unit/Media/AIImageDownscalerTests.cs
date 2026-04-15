using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Umbraco.AI.Core.Media;

namespace Umbraco.AI.Tests.Unit.Media;

public class AIImageDownscalerTests
{
    [Fact]
    public void DownscaleIfNeeded_WhenAutoDownscaleDisabled_ReturnsOriginal()
    {
        var content = CreatePngContent(width: 4000, height: 3000);
        var options = new AIMediaOptions { AutoDownscale = false, MaxDimension = 512 };

        var result = AIImageDownscaler.DownscaleIfNeeded(content, options, NullLogger.Instance);

        result.ShouldBeSameAs(content);
    }

    [Fact]
    public void DownscaleIfNeeded_WhenImageWithinLimits_ReturnsOriginal()
    {
        var content = CreatePngContent(width: 100, height: 100);
        var options = new AIMediaOptions { MaxDimension = 2048, MaxSizeBytes = 10 * 1024 * 1024 };

        var result = AIImageDownscaler.DownscaleIfNeeded(content, options, NullLogger.Instance);

        result.ShouldBeSameAs(content);
    }

    [Fact]
    public void DownscaleIfNeeded_WhenDimensionExceeded_ResizesToLongestEdge()
    {
        // 4000x3000 -> longest edge 4000, limit 2048, ratio 0.512
        var content = CreatePngContent(width: 4000, height: 3000);
        var options = new AIMediaOptions { MaxDimension = 2048, MaxSizeBytes = long.MaxValue };

        var result = AIImageDownscaler.DownscaleIfNeeded(content, options, NullLogger.Instance);

        result.ShouldNotBeSameAs(content);
        result.MediaType.ShouldBe("image/jpeg");

        using var resultImage = Image.Load(result.Data);
        resultImage.Width.ShouldBe(2048);
        resultImage.Height.ShouldBe(1536); // 3000 * 0.512 = 1536
    }

    [Fact]
    public void DownscaleIfNeeded_WithPortraitImage_ResizesByHeight()
    {
        // 1000x3000 -> longest edge 3000 (height), limit 1500
        var content = CreatePngContent(width: 1000, height: 3000);
        var options = new AIMediaOptions { MaxDimension = 1500, MaxSizeBytes = long.MaxValue };

        var result = AIImageDownscaler.DownscaleIfNeeded(content, options, NullLogger.Instance);

        using var resultImage = Image.Load(result.Data);
        resultImage.Height.ShouldBe(1500);
        resultImage.Width.ShouldBe(500);
    }

    [Fact]
    public void DownscaleIfNeeded_WhenByteSizeExceededButDimensionFine_ReencodesWithoutResize()
    {
        // Large PNG (uncompressed noise) that exceeds the byte cap but not the dim cap.
        // A 1500x1500 random-noise PNG will be well over 1 MB.
        var content = CreateNoisePngContent(width: 1500, height: 1500);
        content.Data.Length.ShouldBeGreaterThan(1_000_000);

        var options = new AIMediaOptions { MaxDimension = 4096, MaxSizeBytes = 500_000 };

        var result = AIImageDownscaler.DownscaleIfNeeded(content, options, NullLogger.Instance);

        result.MediaType.ShouldBe("image/jpeg");

        using var resultImage = Image.Load(result.Data);
        resultImage.Width.ShouldBe(1500);
        resultImage.Height.ShouldBe(1500);
        result.Data.Length.ShouldBeLessThan(content.Data.Length);
    }

    [Fact]
    public void DownscaleIfNeeded_WhenAnimated_ReturnsOriginal()
    {
        // Animated GIF with 2 frames, large enough to trip limits.
        var content = CreateAnimatedGifContent(width: 3000, height: 3000, frames: 2);
        var options = new AIMediaOptions { MaxDimension = 512, MaxSizeBytes = 1 };

        var result = AIImageDownscaler.DownscaleIfNeeded(content, options, NullLogger.Instance);

        result.ShouldBeSameAs(content);
    }

    [Fact]
    public void DownscaleIfNeeded_WhenPngHasAlpha_PreservesAsPngAndKeepsTransparency()
    {
        // 4000x3000 Rgba32 with semi-transparent fill — re-encoding to JPEG would
        // silently flatten the alpha channel, so output must stay PNG and keep
        // non-opaque pixels intact.
        var content = CreatePngWithAlphaContent(width: 4000, height: 3000);
        var options = new AIMediaOptions { MaxDimension = 2048, MaxSizeBytes = long.MaxValue };

        var result = AIImageDownscaler.DownscaleIfNeeded(content, options, NullLogger.Instance);

        result.ShouldNotBeSameAs(content);
        result.MediaType.ShouldBe("image/png");

        using var resultImage = Image.Load<Rgba32>(result.Data);
        resultImage.Width.ShouldBe(2048);
        resultImage.Height.ShouldBe(1536);
        // Source fill was A=128; a centre pixel (well away from the single A=0 pixel)
        // should still read as non-opaque after resize.
        resultImage[1024, 768].A.ShouldBeLessThan((byte)255);
    }

    [Fact]
    public void DownscaleIfNeeded_WithCorruptBytes_ReturnsOriginal()
    {
        var content = new AIMediaContent
        {
            Data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE },
            MediaType = "image/png"
        };
        var options = new AIMediaOptions();

        var result = AIImageDownscaler.DownscaleIfNeeded(content, options, NullLogger.Instance);

        result.ShouldBeSameAs(content);
    }

    // Opaque PNG (Rgb24) — round-trips through Image.Load as no-alpha, so the
    // downscaler is free to re-encode as JPEG.
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
    // reference point.
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

    private static AIMediaContent CreateNoisePngContent(int width, int height)
    {
        using var image = new Image<Rgb24>(width, height);
        var random = new Random(42);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgb24(
                        (byte)random.Next(256),
                        (byte)random.Next(256),
                        (byte)random.Next(256));
                }
            }
        });

        using var stream = new MemoryStream();
        image.SaveAsPng(stream, new PngEncoder { CompressionLevel = PngCompressionLevel.NoCompression });
        return new AIMediaContent { Data = stream.ToArray(), MediaType = "image/png" };
    }

    private static AIMediaContent CreateAnimatedGifContent(int width, int height, int frames)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int i = 1; i < frames; i++)
        {
            image.Frames.CreateFrame();
        }

        using var stream = new MemoryStream();
        image.SaveAsGif(stream, new GifEncoder());
        return new AIMediaContent { Data = stream.ToArray(), MediaType = "image/gif" };
    }
}
