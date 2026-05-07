using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Tools;

/// <summary>
/// Event emitted with tool call result.
/// </summary>
public sealed record ToolCallResultEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the message identifier for this result.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets or sets the tool call identifier.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>
    /// Gets or sets the result content.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>
    /// Gets or sets the optional role. AG-UI spec restricts this to the literal <c>"tool"</c>.
    /// </summary>
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; init; } = AGUIConstants.MessageRoles.Tool;
}
