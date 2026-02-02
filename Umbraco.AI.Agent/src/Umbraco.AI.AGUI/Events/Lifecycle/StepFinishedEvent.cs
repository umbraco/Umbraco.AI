using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Lifecycle;

/// <summary>
/// Event emitted when a step within an agent run finishes.
/// </summary>
public sealed record StepFinishedEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the step name.
    /// </summary>
    [JsonPropertyName("stepName")]
    public required string StepName { get; init; }
}
