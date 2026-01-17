using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.Activity;

/// <summary>
/// Event that provides incremental updates to an activity snapshot using JSON Patch (RFC 6902).
/// </summary>
public sealed record ActivityDeltaEvent : BaseAguiEvent
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
    /// Gets or sets the JSON Patch operations to apply.
    /// </summary>
    [JsonPropertyName("patch")]
    public required JsonElement Patch { get; init; }
}
