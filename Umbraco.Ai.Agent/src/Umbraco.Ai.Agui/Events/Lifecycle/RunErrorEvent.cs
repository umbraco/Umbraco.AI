using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Events.Lifecycle;

/// <summary>
/// Event emitted when an agent run encounters an error.
/// </summary>
public sealed record RunErrorEvent : BaseAguiEvent
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the optional error code.
    /// </summary>
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }
}
