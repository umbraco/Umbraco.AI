using System.Text.Json.Serialization;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.AGUI.Events.Lifecycle;

/// <summary>
/// Event emitted when an agent run finishes.
/// </summary>
/// <remarks>
/// <para>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts/events"/>.
/// </para>
/// <para>
/// Per spec the lifecycle terminates with either <c>RUN_FINISHED</c>
/// (success or interrupt) or <c>RUN_ERROR</c> — never both. Errors are
/// signalled exclusively via <see cref="RunErrorEvent"/>.
/// </para>
/// </remarks>
public sealed record RunFinishedEvent : BaseAGUIEvent
{
    /// <summary>
    /// Thread the run belonged to.
    /// </summary>
    [JsonPropertyName("threadId")]
    public required string ThreadId { get; init; }

    /// <summary>
    /// Run identifier.
    /// </summary>
    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    /// <summary>
    /// Outcome of the run: <see cref="AGUIRunOutcomeSuccess"/> or
    /// <see cref="AGUIRunOutcomeInterrupt"/>. The discriminator on the wire
    /// is <c>outcome.type</c>.
    /// </summary>
    [JsonPropertyName("outcome")]
    public required AGUIRunOutcome Outcome { get; init; }

    /// <summary>
    /// Optional terminal result data (spec: any).
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }
}
