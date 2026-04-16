using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Umbraco.AI.Core.Media;

/// <summary>
/// Encodes a decoded <see cref="Image"/> to bytes, choosing a format that
/// preserves alpha when the source has a transparent channel. Shared by the
/// downscaler and cropper so both re-encode paths avoid silently flattening
/// PNG/WebP transparency to an opaque JPEG.
/// </summary>
internal static class AIImageEncoder
{
    /// <summary>
    /// Encodes <paramref name="image"/> to PNG when it carries an alpha channel
    /// (lossless, transparency preserved), otherwise to JPEG at the given
    /// quality (smaller payload for opaque/photographic content).
    /// </summary>
    public static AIMediaContent Encode(Image image, int jpegQuality = 85)
    {
        using var output = new MemoryStream();

        if (HasAlpha(image))
        {
            image.SaveAsPng(output);
            return new AIMediaContent
            {
                Data = output.ToArray(),
                MediaType = "image/png"
            };
        }

        image.SaveAsJpeg(output, new JpegEncoder { Quality = jpegQuality });
        return new AIMediaContent
        {
            Data = output.ToArray(),
            MediaType = "image/jpeg"
        };
    }

    private static bool HasAlpha(Image image)
    {
        // Prefer the explicit AlphaRepresentation when the decoder populated it.
        var alpha = image.PixelType.AlphaRepresentation;
        if (alpha is PixelAlphaRepresentation.Associated or PixelAlphaRepresentation.Unassociated)
        {
            return true;
        }
        if (alpha is PixelAlphaRepresentation.None)
        {
            return false;
        }

        // AlphaRepresentation is nullable and not every ImageSharp decoder populates it
        // (notably the PNG decoder leaves it null even for 32-bpp RGBA images). Fall
        // back to the runtime pixel type — any of these formats carries an alpha channel.
        return image is Image<Rgba32> or Image<Bgra32> or Image<Rgba64> or Image<La16> or Image<La32>;
    }
}
