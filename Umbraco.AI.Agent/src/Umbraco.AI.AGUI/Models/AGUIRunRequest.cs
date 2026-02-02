using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Represents a run request in the AG-UI protocol.
/// </summary>
public sealed class AGUIRunRequest
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
    public IEnumerable<AGUIMessage> Messages { get; set; } = [];

    /// <summary>
    /// Gets or sets the available tools.
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<AGUITool>? Tools { get; set; }

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
    public IEnumerable<AGUIContextItem>? Context { get; set; }

    /// <summary>
    /// Gets or sets the resume information for continuing from an interrupt.
    /// </summary>
    [JsonPropertyName("resume")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AGUIResumeInfo? Resume { get; set; }

    /// <summary>
    /// Gets or sets additional properties to forward to the agent.
    /// </summary>
    [JsonPropertyName("forwardedProps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? ForwardedProps { get; set; }
}
