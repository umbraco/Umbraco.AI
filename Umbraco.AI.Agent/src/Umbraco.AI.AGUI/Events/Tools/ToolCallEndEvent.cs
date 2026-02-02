using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Tools;

/// <summary>
/// Event emitted when a tool call ends.
/// </summary>
public sealed record ToolCallEndEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the tool call identifier.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }
}
