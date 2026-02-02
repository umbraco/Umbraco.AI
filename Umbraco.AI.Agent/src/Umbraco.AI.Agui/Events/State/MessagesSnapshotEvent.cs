using System.Text.Json.Serialization;
using Umbraco.AI.Agui.Models;

namespace Umbraco.AI.Agui.Events.State;

/// <summary>
/// Event emitted with a full messages snapshot.
/// </summary>
public sealed record MessagesSnapshotEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the messages snapshot.
    /// </summary>
    [JsonPropertyName("messages")]
    public required IEnumerable<AguiMessage> Messages { get; init; }
}
