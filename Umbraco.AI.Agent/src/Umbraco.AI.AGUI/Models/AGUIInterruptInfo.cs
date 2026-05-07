using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Describes a human-in-the-loop interrupt that paused an agent run.
/// </summary>
/// <remarks>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts/interrupts"/>.
/// </remarks>
public sealed class AGUIInterruptInfo
{
    /// <summary>
    /// Correlation key across interrupt, resume, idempotency, and audit.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Categorical routing hint. AG-UI core values: <c>tool_call</c>, <c>input_required</c>,
    /// <c>confirmation</c>, or a custom <c>&lt;framework&gt;:&lt;name&gt;</c> namespaced reason.
    /// </summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; set; }

    /// <summary>
    /// Human-readable prompt — the universal fallback UI content.
    /// </summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>
    /// Binds the interrupt to a prior tool call proposal when applicable.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    /// <summary>
    /// JSON Schema validating the resume payload structure.
    /// </summary>
    [JsonPropertyName("responseSchema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ResponseSchema { get; set; }

    /// <summary>
    /// Optional ISO-8601 expiry — stale resumes trigger errors.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpiresAt { get; set; }

    /// <summary>
    /// Framework-specific extension data.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Metadata { get; set; }
}
