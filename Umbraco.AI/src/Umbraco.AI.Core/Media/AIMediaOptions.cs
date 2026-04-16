namespace Umbraco.AI.Core.Media;

/// <summary>
/// Configuration options for AI media resolution, including automatic
/// downscaling of oversized images before they are sent to AI providers.
/// </summary>
public class AIMediaOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether oversized images are automatically
    /// downscaled before being sent to AI providers. Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Most providers enforce hard limits on image payload size and/or pixel dimensions
    /// (e.g. Anthropic Claude rejects images above 5 MB). Leaving this enabled protects
    /// against those limits at the cost of a single re-encode for oversized inputs.
    /// </remarks>
    public bool AutoDownscale { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum allowed byte size of an image payload after resolution.
    /// Images above this size are re-encoded. Default is 4 MB.
    /// </summary>
    public long MaxSizeBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum allowed pixel dimension (longest edge) for an image.
    /// Images above this dimension are resized proportionally. Default is 2048.
    /// </summary>
    public int MaxDimension { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the JPEG quality (1-100) used when re-encoding oversized images.
    /// Lower values produce smaller files at the cost of visual fidelity. Default is 85.
    /// </summary>
    public int JpegQuality { get; set; } = 85;
}
