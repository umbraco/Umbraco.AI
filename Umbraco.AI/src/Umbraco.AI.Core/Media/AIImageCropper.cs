using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Umbraco.AI.Core.Media;

/// <summary>
/// Applies named image cropper crops to raw image bytes. Used when a prompt
/// template requests a specific crop via the <c>#cropAlias</c> suffix.
/// </summary>
internal static class AIImageCropper
{
    /// <summary>
    /// Returns a copy of <paramref name="content"/> cropped to the named crop's
    /// coordinates and resized to the crop's target dimensions. When the named
    /// crop is missing, has no coordinates, or the image cannot be decoded, the
    /// original content is returned unchanged.
    /// </summary>
    /// <param name="content">Source image bytes.</param>
    /// <param name="crops">Crop definitions from the image cropper payload.</param>
    /// <param name="cropAlias">Alias of the crop to apply.</param>
    /// <param name="logger">Logger for diagnostic warnings.</param>
    public static AIMediaContent ApplyCrop(
        AIMediaContent content,
        IReadOnlyList<ImageCropperCrop> crops,
        string cropAlias,
        ILogger logger)
    {
        var crop = crops.FirstOrDefault(
            c => string.Equals(c.Alias, cropAlias, StringComparison.OrdinalIgnoreCase));

        if (crop is null)
        {
            logger.LogWarning(
                "Image cropper value has no crop with alias '{CropAlias}'; using original image",
                cropAlias);
            return content;
        }

        if (crop.Coordinates is null)
        {
            logger.LogWarning(
                "Crop '{CropAlias}' has no coordinates defined; using original image",
                cropAlias);
            return content;
        }

        Image image;
        try
        {
            image = Image.Load(content.Data);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decode image for crop '{CropAlias}'; using original", cropAlias);
            return content;
        }

        using (image)
        {
            var coords = crop.Coordinates;

            // Umbraco's image cropper stores coordinates as 0..1 fractions but with
            // an asymmetric meaning: X1/Y1 are offsets from the left/top edges, while
            // X2/Y2 are offsets from the *right* and *bottom* edges — not absolute
            // positions. So the real crop rectangle edges are:
            //
            //   left   = X1
            //   top    = Y1
            //   right  = 1 - X2
            //   bottom = 1 - Y2
            //
            // Matches Umbraco.Cms.Imaging.ImageSharp.ImageProcessors.CropWebProcessor.
            var left = Math.Clamp((double)coords.X1, 0, 1);
            var top = Math.Clamp((double)coords.Y1, 0, 1);
            var right = Math.Clamp(1 - (double)coords.X2, 0, 1);
            var bottom = Math.Clamp(1 - (double)coords.Y2, 0, 1);

            var x = (int)Math.Round(left * image.Width);
            var y = (int)Math.Round(top * image.Height);
            var w = (int)Math.Round((right - left) * image.Width);
            var h = (int)Math.Round((bottom - top) * image.Height);

            // Clamp to image bounds defensively — malformed coordinates shouldn't crash.
            x = Math.Clamp(x, 0, Math.Max(0, image.Width - 1));
            y = Math.Clamp(y, 0, Math.Max(0, image.Height - 1));
            w = Math.Clamp(w, 1, image.Width - x);
            h = Math.Clamp(h, 1, image.Height - y);

            image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, w, h)));

            // Resize to the crop's declared target dimensions when specified.
            if (crop.Width > 0 && crop.Height > 0)
            {
                image.Mutate(ctx => ctx.Resize(crop.Width, crop.Height));
            }

            return AIImageEncoder.Encode(image, jpegQuality: 90);
        }
    }
}
