using System.Text.Json.Serialization;

namespace Umbraco.AI.Agui.Models;

/// <summary>
/// Represents a tool call in the AG-UI protocol.
/// </summary>
public sealed class AguiToolCall
{
    /// <summary>
    /// Gets or sets the tool call identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of tool call (always "function").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Gets or sets the function details.
    /// </summary>
    [JsonPropertyName("function")]
    public required AguiFunctionCall Function { get; set; }
}
