using System.Text.Json.Serialization;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.AGUI.Events.State;

/// <summary>
/// Event emitted with a full messages snapshot.
/// </summary>
public sealed record MessagesSnapshotEvent : BaseAGUIEvent
{
    /// <summary>
    /// Gets or sets the messages snapshot.
    /// </summary>
    [JsonPropertyName("messages")]
    public required IEnumerable<AGUIMessage> Messages { get; init; }
}
