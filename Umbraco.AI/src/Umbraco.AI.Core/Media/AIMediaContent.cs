namespace Umbraco.AI.Core.Media;

/// <summary>
/// Represents resolved media content with binary data and media type.
/// </summary>
public sealed class AIMediaContent
{
    /// <summary>
    /// Gets or sets the media binary data.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Gets or sets the MIME type of the media (e.g., "image/jpeg", "audio/mpeg").
    /// </summary>
    public required string MediaType { get; init; }

    /// <summary>
    /// Gets the Umbraco media node key when the content was resolved from a media reference
    /// (GUID or media picker value). Null when the content was resolved from a raw file path —
    /// in that case the source is not a CMS media node and no per-node authorisation applies.
    /// </summary>
    public Guid? MediaKey { get; init; }
}
