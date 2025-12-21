using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Models;

/// <summary>
/// Represents a run request in the AG-UI protocol.
/// </summary>
public sealed class AguiRunRequest
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the run identifier.
    /// </summary>
    [JsonPropertyName("runId")]
    public string RunId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the messages in the conversation.
    /// </summary>
    [JsonPropertyName("messages")]
    public IEnumerable<AguiMessage> Messages { get; set; } = [];

    /// <summary>
    /// Gets or sets the available tools.
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<AguiTool>? Tools { get; set; }

    /// <summary>
    /// Gets or sets the current state.
    /// </summary>
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? State { get; set; }

    /// <summary>
    /// Gets or sets the context items.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<AguiContextItem>? Context { get; set; }

    /// <summary>
    /// Gets or sets the resume information for continuing from an interrupt.
    /// </summary>
    [JsonPropertyName("resume")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AguiResumeInfo? Resume { get; set; }

    /// <summary>
    /// Gets or sets additional properties to forward to the agent.
    /// </summary>
    [JsonPropertyName("forwardedProps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? ForwardedProps { get; set; }
}
