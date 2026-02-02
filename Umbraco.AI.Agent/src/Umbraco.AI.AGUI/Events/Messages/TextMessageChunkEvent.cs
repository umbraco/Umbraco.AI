using System.Text.Json.Serialization;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.AGUI.Events.Messages;

/// <summary>
/// Convenience event that combines message start, content, and end into a single event.
/// Useful for simpler streaming scenarios.
/// </summary>
public sealed record TextMessageChunkEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the optional message identifier.
    /// </summary>
    [JsonPropertyName("messageId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets or sets the optional message role.
    /// </summary>
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AGUIMessageRole? Role { get; init; }

    /// <summary>
    /// Gets or sets the optional content delta.
    /// </summary>
    [JsonPropertyName("delta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Delta { get; init; }
}
