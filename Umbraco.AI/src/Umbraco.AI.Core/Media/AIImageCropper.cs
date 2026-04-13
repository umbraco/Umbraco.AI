using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Umbraco.AI.Prompt.Core.Media;

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

            // Coordinates are proportional (0..1) in the image cropper JSON format.
            var x = (int)Math.Round((double)coords.X1 * image.Width);
            var y = (int)Math.Round((double)coords.Y1 * image.Height);
            var w = (int)Math.Round((double)(coords.X2 - coords.X1) * image.Width);
            var h = (int)Math.Round((double)(coords.Y2 - coords.Y1) * image.Height);

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

            using var output = new MemoryStream();
            image.SaveAsJpeg(output, new JpegEncoder { Quality = 90 });

            return new AIMediaContent
            {
                Data = output.ToArray(),
                MediaType = "image/jpeg"
            };
        }
    }
}
