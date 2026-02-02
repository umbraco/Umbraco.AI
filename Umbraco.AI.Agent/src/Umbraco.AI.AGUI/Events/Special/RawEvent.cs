using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Special;

/// <summary>
/// Raw event for passing through unprocessed data.
/// </summary>
public sealed record RawEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the raw event data.
    /// </summary>
    [JsonPropertyName("event")]
    public required JsonElement Event { get; init; }

    /// <summary>
    /// Gets or sets the optional source identifier.
    /// </summary>
    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }
}
