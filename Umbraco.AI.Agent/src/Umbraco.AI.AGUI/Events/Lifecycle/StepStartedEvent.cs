using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Events.Lifecycle;

/// <summary>
/// Event emitted when a step within an agent run starts.
/// </summary>
public sealed record StepStartedEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the step name.
    /// </summary>
    [JsonPropertyName("stepName")]
    public required string StepName { get; init; }
}
