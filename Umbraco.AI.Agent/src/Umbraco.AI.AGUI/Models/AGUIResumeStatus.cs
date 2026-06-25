using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Resolution status of an interrupt-resume entry, per AG-UI spec.
/// </summary>
/// <remarks>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts/interrupts"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<AGUIResumeStatus>))]
public enum AGUIResumeStatus
{
    /// <summary>
    /// User responded to the interrupt; <see cref="AGUIResumeEntry.Payload"/>
    /// contains the resume response (e.g., the tool result).
    /// </summary>
    [JsonStringEnumMemberName("resolved")]
    Resolved,

    /// <summary>
    /// User abandoned the interrupt without input; payload is omitted.
    /// </summary>
    [JsonStringEnumMemberName("cancelled")]
    Cancelled
}
