using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Lifecycle;

/// <summary>
/// Event emitted when an agent run encounters an error.
/// </summary>
public sealed record RunErrorEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the optional error code.
    /// </summary>
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }
}
