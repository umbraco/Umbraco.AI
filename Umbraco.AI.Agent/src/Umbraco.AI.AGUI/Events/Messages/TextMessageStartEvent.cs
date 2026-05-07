using System.Text.Json.Serialization;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.AGUI.Events.Messages;

/// <summary>
/// Event emitted when a text message starts streaming.
/// </summary>
public sealed record TextMessageStartEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets or sets the message role. Restricted by spec to a subset of roles
    /// (no <c>tool</c>, <c>activity</c>, or <c>reasoning</c>).
    /// </summary>
    [JsonPropertyName("role")]
    public required AGUITextMessageRole Role { get; init; }

    /// <summary>
    /// Optional sender name (spec field).
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }
}
