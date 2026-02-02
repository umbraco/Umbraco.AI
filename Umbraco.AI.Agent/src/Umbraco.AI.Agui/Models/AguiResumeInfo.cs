using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Agui.Models;

/// <summary>
/// Information for resuming from a human-in-the-loop interrupt.
/// </summary>
public sealed class AguiResumeInfo
{
    /// <summary>
    /// Gets or sets the interrupt identifier to resume from.
    /// </summary>
    [JsonPropertyName("interruptId")]
    public string InterruptId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's response payload (approvals, edits, file references, etc.).
    /// </summary>
    [JsonPropertyName("payload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Payload { get; set; }
}
