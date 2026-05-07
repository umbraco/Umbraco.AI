using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Activity;

/// <summary>
/// Event that provides incremental updates to an activity snapshot using JSON Patch (RFC 6902).
/// </summary>
public sealed record ActivityDeltaEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets or sets the activity type.
    /// </summary>
    [JsonPropertyName("activityType")]
    public required string ActivityType { get; init; }

    /// <summary>
    /// Gets or sets the JSON Patch operations to apply (RFC 6902).
    /// AG-UI spec types this as an array — enforced here as <c>IReadOnlyList&lt;JsonElement&gt;</c>.
    /// </summary>
    [JsonPropertyName("patch")]
    public required IReadOnlyList<JsonElement> Patch { get; init; }
}
