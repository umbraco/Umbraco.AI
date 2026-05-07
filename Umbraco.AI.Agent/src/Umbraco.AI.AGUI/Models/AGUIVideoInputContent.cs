using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Video content part for AG-UI multimodal messages (<c>video/*</c> mime types).
/// </summary>
public sealed class AGUIVideoInputContent : AGUIInputContent
{
    /// <summary>
    /// Source of the video (inline data or URL reference).
    /// </summary>
    [JsonPropertyName("source")]
    public required AGUIInputContentSource Source { get; set; }

    /// <summary>
    /// Optional metadata bag (e.g., <c>filename</c>).
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Metadata { get; set; }
}
