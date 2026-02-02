namespace Umbraco.Ai.Prompt.Core.Media;

/// <summary>
/// Represents resolved media content with binary data and media type.
/// </summary>
public sealed class AiMediaContent
{
    /// <summary>
    /// Gets or sets the media binary data.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Gets or sets the MIME type of the media (e.g., "image/jpeg", "image/png").
    /// </summary>
    public required string MediaType { get; init; }
}
