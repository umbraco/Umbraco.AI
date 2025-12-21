using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.Lifecycle;

/// <summary>
/// Event emitted when an agent run starts.
/// </summary>
public sealed record RunStartedEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the thread identifier.
    /// </summary>
    [JsonPropertyName("threadId")]
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets or sets the run identifier.
    /// </summary>
    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    /// <summary>
    /// Gets or sets the optional parent run identifier for nested/child runs.
    /// </summary>
    [JsonPropertyName("parentRunId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentRunId { get; init; }

    /// <summary>
    /// Gets or sets the optional input data for the run.
    /// </summary>
    [JsonPropertyName("input")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Input { get; init; }
}
