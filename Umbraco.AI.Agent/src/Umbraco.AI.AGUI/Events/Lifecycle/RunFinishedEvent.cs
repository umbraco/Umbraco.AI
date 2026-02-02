using System.Text.Json.Serialization;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.AGUI.Events.Lifecycle;

/// <summary>
/// Event emitted when an agent run finishes.
/// </summary>
public sealed record RunFinishedEvent : BaseAGUIEvent
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
    /// Gets or sets the run outcome.
    /// </summary>
    [JsonPropertyName("outcome")]
    public AGUIRunOutcome Outcome { get; init; } = AGUIRunOutcome.Success;

    /// <summary>
    /// Gets or sets the interrupt information when outcome is Interrupt.
    /// </summary>
    [JsonPropertyName("interrupt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AGUIInterruptInfo? Interrupt { get; init; }

    /// <summary>
    /// Gets or sets the optional result data.
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }
}
