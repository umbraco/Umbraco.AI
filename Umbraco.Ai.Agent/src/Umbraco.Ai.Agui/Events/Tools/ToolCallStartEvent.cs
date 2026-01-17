using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.Tools;

/// <summary>
/// Event emitted when a tool call starts.
/// </summary>
public sealed record ToolCallStartEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the tool call identifier.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>
    /// Gets or sets the tool call name.
    /// </summary>
    [JsonPropertyName("toolCallName")]
    public required string ToolCallName { get; init; }

    /// <summary>
    /// Gets or sets the optional parent message identifier.
    /// </summary>
    [JsonPropertyName("parentMessageId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentMessageId { get; init; }
}
