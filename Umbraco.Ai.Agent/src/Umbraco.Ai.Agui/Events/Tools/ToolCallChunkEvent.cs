using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.Tools;

/// <summary>
/// Convenience event that combines tool call start, args, and end into a single event.
/// Useful for simpler streaming scenarios.
/// </summary>
public sealed record ToolCallChunkEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the optional tool call identifier.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; init; }

    /// <summary>
    /// Gets or sets the optional tool call name.
    /// </summary>
    [JsonPropertyName("toolCallName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallName { get; init; }

    /// <summary>
    /// Gets or sets the optional parent message identifier.
    /// </summary>
    [JsonPropertyName("parentMessageId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentMessageId { get; init; }

    /// <summary>
    /// Gets or sets the optional arguments delta.
    /// </summary>
    [JsonPropertyName("delta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Delta { get; init; }
}
