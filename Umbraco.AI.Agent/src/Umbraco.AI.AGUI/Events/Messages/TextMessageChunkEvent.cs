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
    /// Gets or sets the optional message role. Restricted by spec to a subset of roles
    /// (no <c>tool</c>, <c>activity</c>, or <c>reasoning</c>).
    /// </summary>
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AGUITextMessageRole? Role { get; init; }

    /// <summary>
    /// Gets or sets the optional content delta.
    /// </summary>
    [JsonPropertyName("delta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Delta { get; init; }

    /// <summary>
    /// Optional sender name (spec field).
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }
}
