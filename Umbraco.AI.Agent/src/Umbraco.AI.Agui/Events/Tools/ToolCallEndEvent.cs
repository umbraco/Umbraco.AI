using System.Text.Json.Serialization;

namespace Umbraco.AI.Agui.Events.Tools;

/// <summary>
/// Event emitted when a tool call ends.
/// </summary>
public sealed record ToolCallEndEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the tool call identifier.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }
}
