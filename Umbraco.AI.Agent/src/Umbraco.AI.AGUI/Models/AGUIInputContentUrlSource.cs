using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// URL-based content source — value is a URL the consumer can fetch.
/// </summary>
public sealed class AGUIInputContentUrlSource : AGUIInputContentSource
{
    /// <summary>
    /// URL where the content can be retrieved.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }

    /// <summary>
    /// Optional MIME type hint.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }
}
