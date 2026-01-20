namespace Umbraco.Ai.Prompt.Core.Media;

/// <summary>
/// Represents resolved image content with binary data and media type.
/// </summary>
public sealed class AiImageContent
{
    /// <summary>
    /// Gets or sets the image binary data.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Gets or sets the MIME type of the image (e.g., "image/jpeg", "image/png").
    /// </summary>
    public required string MediaType { get; init; }
}
