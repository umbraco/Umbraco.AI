using System.Text.Json.Serialization;
using Umbraco.Ai.Agui.Models;

namespace Umbraco.Ai.Agui.Events.Lifecycle;

/// <summary>
/// Event emitted when an agent run finishes.
/// </summary>
public sealed record RunFinishedEvent : BaseAguiEvent
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
    public AguiRunOutcome Outcome { get; init; } = AguiRunOutcome.Success;

    /// <summary>
    /// Gets or sets the interrupt information when outcome is Interrupt.
    /// </summary>
    [JsonPropertyName("interrupt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AguiInterruptInfo? Interrupt { get; init; }

    /// <summary>
    /// Gets or sets the optional result data.
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }
}
