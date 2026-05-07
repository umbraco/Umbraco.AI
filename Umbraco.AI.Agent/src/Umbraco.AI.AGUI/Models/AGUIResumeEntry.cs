using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Resume entry for continuing from a single human-in-the-loop interrupt.
/// </summary>
/// <remarks>
/// <para>
/// Per AG-UI spec, the client sends one entry per open interrupt in
/// <see cref="AGUIRunRequest.Resume"/>. The <see cref="InterruptId"/> correlates
/// with the <c>id</c> of the interrupt the server emitted.
/// </para>
/// <para>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts/interrupts"/>.
/// </para>
/// </remarks>
public sealed class AGUIResumeEntry
{
    /// <summary>
    /// Correlates with the <c>id</c> of the original interrupt.
    /// </summary>
    [JsonPropertyName("interruptId")]
    public required string InterruptId { get; set; }

    /// <summary>
    /// How the interrupt was resolved.
    /// </summary>
    [JsonPropertyName("status")]
    public required AGUIResumeStatus Status { get; set; }

    /// <summary>
    /// Resume response data when <see cref="Status"/> is <see cref="AGUIResumeStatus.Resolved"/>.
    /// For tool-call interrupts this is the tool result; for confirmation interrupts it
    /// is the chosen response (e.g., <c>{ approved: true }</c>). Omitted when cancelled.
    /// </summary>
    [JsonPropertyName("payload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Payload { get; set; }
}
