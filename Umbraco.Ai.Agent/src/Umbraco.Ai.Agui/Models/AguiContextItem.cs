using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Models;

/// <summary>
/// Represents a context item in the AG-UI protocol.
/// </summary>
public sealed class AguiContextItem
{
    /// <summary>
    /// Gets or sets the context item description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the context item value.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
