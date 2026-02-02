using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Models;

/// <summary>
/// Represents function call details in a tool call.
/// </summary>
public sealed class AguiFunctionCall
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function arguments as a JSON string.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}
