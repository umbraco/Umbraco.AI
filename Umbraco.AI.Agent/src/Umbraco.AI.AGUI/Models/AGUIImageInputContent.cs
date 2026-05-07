using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Image content part for AG-UI multimodal messages (<c>image/*</c> mime types).
/// </summary>
public sealed class AGUIImageInputContent : AGUIInputContent
{
    /// <summary>
    /// Source of the image (inline data or URL reference).
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
