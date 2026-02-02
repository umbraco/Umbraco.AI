using System.Text.Json.Serialization;

namespace Umbraco.AI.Agui.Models;

/// <summary>
/// Information about a human-in-the-loop interrupt.
/// </summary>
public sealed class AguiInterruptInfo
{
    /// <summary>
    /// Gets or sets the interrupt identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the interrupt.
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional payload data for the interrupt.
    /// </summary>
    [JsonPropertyName("payload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Payload { get; set; }
}
