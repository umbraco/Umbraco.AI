using System.Text.Json.Serialization;
using Umbraco.Ai.Agui.Models;

namespace Umbraco.Ai.Agui.Events.Tools;

/// <summary>
/// Event emitted with tool call result.
/// </summary>
public sealed record ToolCallResultEvent : BaseAguiEvent
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
    /// Gets or sets the optional role (typically "tool").
    /// </summary>
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AguiMessageRole? Role { get; init; }
}
