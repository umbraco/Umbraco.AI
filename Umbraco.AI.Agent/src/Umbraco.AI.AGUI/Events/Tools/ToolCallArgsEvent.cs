using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Tools;

/// <summary>
/// Event emitted for tool call argument deltas.
/// </summary>
public sealed record ToolCallArgsEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the tool call identifier.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>
    /// Gets or sets the arguments delta (JSON string fragment).
    /// </summary>
    [JsonPropertyName("delta")]
    public required string Delta { get; init; }
}
