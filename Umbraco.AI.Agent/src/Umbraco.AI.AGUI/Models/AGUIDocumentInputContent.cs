using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Document content part for AG-UI multimodal messages — the catch-all for non-media
/// MIME types (PDFs, ZIPs, plain text, Office docs, etc.).
/// </summary>
public sealed class AGUIDocumentInputContent : AGUIInputContent
{
    /// <summary>
    /// Source of the document (inline data or URL reference).
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
