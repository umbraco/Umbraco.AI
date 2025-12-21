using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.Activity;

/// <summary>
/// Event that delivers a complete snapshot of an activity message.
/// Activity messages are frontend-only UI updates that don't affect the conversation history.
/// </summary>
public sealed record ActivitySnapshotEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets or sets the activity type (e.g., "thinking", "searching", "processing").
    /// </summary>
    [JsonPropertyName("activityType")]
    public required string ActivityType { get; init; }

    /// <summary>
    /// Gets or sets the activity content.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>
    /// Gets or sets whether this snapshot should replace any existing activity with the same messageId.
    /// </summary>
    [JsonPropertyName("replace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Replace { get; init; }
}
