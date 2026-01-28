using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.Special;

/// <summary>
/// Custom event for application-specific data.
/// </summary>
public sealed record CustomEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the custom event name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the custom event value.
    /// </summary>
    [JsonPropertyName("value")]
    public required object Value { get; init; }
}
