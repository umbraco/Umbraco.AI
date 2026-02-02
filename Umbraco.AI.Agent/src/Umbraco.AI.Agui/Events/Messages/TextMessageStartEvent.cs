using System.Text.Json.Serialization;
using Umbraco.Ai.Agui.Models;

namespace Umbraco.Ai.Agui.Events.Messages;

/// <summary>
/// Event emitted when a text message starts streaming.
/// </summary>
public sealed record TextMessageStartEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets or sets the message role.
    /// </summary>
    [JsonPropertyName("role")]
    public required AguiMessageRole Role { get; init; }
}
