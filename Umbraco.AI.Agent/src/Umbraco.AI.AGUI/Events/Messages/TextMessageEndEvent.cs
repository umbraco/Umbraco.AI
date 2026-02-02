using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Messages;

/// <summary>
/// Event emitted when a text message finishes streaming.
/// </summary>
public sealed record TextMessageEndEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }
}
