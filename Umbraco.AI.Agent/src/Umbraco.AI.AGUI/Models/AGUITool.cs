using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Represents a tool definition in the AG-UI protocol.
/// </summary>
/// <remarks>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts"/>. Per spec,
/// <c>parameters</c> is an open <c>any</c> (typically a JSON Schema), and
/// <c>metadata</c> is an optional <c>Record&lt;string, any&gt;</c> for
/// vendor-specific tool data (e.g., scope, isDestructive).
/// </remarks>
public sealed class AGUITool
{
    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tool description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tool parameters schema. Per AG-UI docs this is a JSON Schema
    /// document describing the structure of arguments the tool accepts — used by the
    /// agent to generate valid tool calls and by the frontend to validate them. The
    /// AG-UI wire layer types this as <c>any</c>, so the full JSON Schema vocabulary
    /// (<c>oneOf</c>, <c>enum</c>, <c>$ref</c>, etc.) is supported. Optional per spec.
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Parameters { get; set; }

    /// <summary>
    /// Gets or sets vendor-specific metadata. Replaces the previous
    /// <c>forwardedProps.toolMetadata</c> side-channel — scope and isDestructive
    /// now travel inline with each tool definition.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Metadata { get; set; }
}
