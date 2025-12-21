using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Models;

/// <summary>
/// Represents a message in the AG-UI protocol.
/// </summary>
public sealed class AguiMessage
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the message role.
    /// </summary>
    [JsonPropertyName("role")]
    public AguiMessageRole Role { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the optional name (for tool messages).
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tool calls made by the assistant.
    /// </summary>
    [JsonPropertyName("toolCalls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<AguiToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the tool call ID this message is responding to.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }
}
