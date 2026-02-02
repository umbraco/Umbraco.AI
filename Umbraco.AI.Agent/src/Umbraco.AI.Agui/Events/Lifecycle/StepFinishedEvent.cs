using System.Text.Json.Serialization;

namespace Umbraco.AI.Agui.Events.Lifecycle;

/// <summary>
/// Event emitted when a step within an agent run finishes.
/// </summary>
public sealed record StepFinishedEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the step name.
    /// </summary>
    [JsonPropertyName("stepName")]
    public required string StepName { get; init; }
}
