using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.State;

/// <summary>
/// Event emitted with a full state snapshot.
/// </summary>
public sealed record StateSnapshotEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the state snapshot.
    /// </summary>
    [JsonPropertyName("snapshot")]
    public required JsonElement Snapshot { get; init; }
}
