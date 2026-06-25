using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Activity;

/// <summary>
/// Event that delivers a complete snapshot of an activity message.
/// Activity messages are frontend-only UI updates that don't affect the conversation history.
/// </summary>
/// <remarks>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts/events"/>.
/// </remarks>
public sealed record ActivitySnapshotEvent : BaseAGUIEvent
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
    /// Gets or sets the activity content as a JSON object (spec: <c>Record&lt;string, any&gt;</c>).
    /// </summary>
    [JsonPropertyName("content")]
    public required JsonElement Content { get; init; }

    /// <summary>
    /// Gets or sets whether this snapshot should replace any existing activity with the same messageId.
    /// </summary>
    [JsonPropertyName("replace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Replace { get; init; }
}
