using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Agui.Models;

/// <summary>
/// Represents a tool definition in the AG-UI protocol.
/// </summary>
public sealed class AguiTool
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
    /// Gets or sets the tool parameters schema.
    /// </summary>
    [JsonPropertyName("parameters")]
    public AguiToolParameters Parameters { get; set; } = new();
}

/// <summary>
/// Represents the parameters schema for a tool.
/// </summary>
public sealed class AguiToolParameters
{
    /// <summary>
    /// Gets or sets the schema type (typically "object").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    /// <summary>
    /// Gets or sets the properties schema.
    /// </summary>
    [JsonPropertyName("properties")]
    public JsonElement Properties { get; set; }

    /// <summary>
    /// Gets or sets the required property names.
    /// </summary>
    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Required { get; set; }
}
