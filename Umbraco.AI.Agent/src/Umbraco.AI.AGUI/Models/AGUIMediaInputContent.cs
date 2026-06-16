using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Common shape for AG-UI typed media content variants (image / audio / video / document).
/// Each variant is a sealed leaf that adds only its <c>type</c> discriminator.
/// </summary>
public abstract class AGUIMediaInputContent : AGUIInputContent
{
    /// <summary>
    /// Source of the content (inline data or URL reference).
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
