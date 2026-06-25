using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Inline-data content source — base64-encoded value with declared MIME type.
/// </summary>
public sealed class AGUIInputContentDataSource : AGUIInputContentSource
{
    /// <summary>
    /// Base64-encoded content bytes.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }

    /// <summary>
    /// MIME type of the content (e.g., <c>image/png</c>, <c>application/pdf</c>).
    /// </summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; set; }
}
