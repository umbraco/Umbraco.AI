using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Umbraco.AI.Core.Media;

/// <summary>
/// Provides image downscaling used by the media resolver to keep payloads
/// within AI provider limits. Pure function over byte data — no file IO.
/// </summary>
internal static class AIImageDownscaler
{
    /// <summary>
    /// Returns a downscaled copy of <paramref name="content"/> when it exceeds the
    /// byte or dimension thresholds in <paramref name="options"/>, or the original
    /// content when it is within limits, animated, or cannot be decoded.
    /// </summary>
    public static AIMediaContent DownscaleIfNeeded(
        AIMediaContent content,
        AIMediaOptions options,
        ILogger logger,
        string pathForLogging = "")
    {
        if (!options.AutoDownscale)
        {
            return content;
        }

        // Cheap header-only read to inspect dimensions without decoding pixels.
        ImageInfo? info;
        try
        {
            info = Image.Identify(content.Data);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to inspect image header for {Path}; passing through unchanged", pathForLogging);
            return content;
        }

        if (info is null)
        {
            // Unrecognised format — let the provider decide what to do with it.
            return content;
        }

        var longestEdge = Math.Max(info.Width, info.Height);
        var oversizedBytes = content.Data.LongLength > options.MaxSizeBytes;
        var oversizedDimension = longestEdge > options.MaxDimension;

        if (!oversizedBytes && !oversizedDimension)
        {
            return content;
        }

        Image image;
        try
        {
            image = Image.Load(content.Data);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decode image {Path} for downscale; passing through unchanged", pathForLogging);
            return content;
        }

        using (image)
        {
            // Animated images (GIF/WebP/APNG) would lose animation if re-encoded to JPEG.
            // Pass through and let the provider handle/reject as it sees fit.
            if (image.Frames.Count > 1)
            {
                logger.LogWarning(
                    "Image {Path} exceeds limits ({Bytes} bytes, {LongestEdge}px) but is animated ({Frames} frames); passing through unchanged",
                    pathForLogging, content.Data.Length, longestEdge, image.Frames.Count);
                return content;
            }

            if (oversizedDimension)
            {
                var ratio = (double)options.MaxDimension / longestEdge;
                var newWidth = Math.Max(1, (int)Math.Round(image.Width * ratio));
                var newHeight = Math.Max(1, (int)Math.Round(image.Height * ratio));
                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            var resized = AIImageEncoder.Encode(image, options.JpegQuality);

            logger.LogDebug(
                "Downscaled image {Path}: {OldBytes} bytes ({OldDim}px) -> {NewBytes} bytes ({NewDim}px, {Format})",
                pathForLogging,
                content.Data.Length, longestEdge,
                resized.Data.Length, Math.Max(image.Width, image.Height), resized.MediaType);

            return resized;
        }
    }
}
