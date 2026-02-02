using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Agui.Events.State;

/// <summary>
/// Event emitted with a state delta (JSON Patch).
/// </summary>
public sealed record StateDeltaEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the JSON Patch operations (RFC 6902).
    /// </summary>
    [JsonPropertyName("delta")]
    public required JsonElement Delta { get; init; }
}
