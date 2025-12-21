using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.Messages;

/// <summary>
/// Event emitted for text message content deltas.
/// </summary>
public sealed record TextMessageContentEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets or sets the content delta.
    /// </summary>
    [JsonPropertyName("delta")]
    public required string Delta { get; init; }
}
