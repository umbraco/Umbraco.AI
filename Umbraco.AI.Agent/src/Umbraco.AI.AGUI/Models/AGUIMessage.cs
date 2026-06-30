using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Represents a message in the AG-UI protocol.
/// Supports both plain string content and multimodal content parts (AG-UI multimodal messages draft).
/// </summary>
/// <remarks>
/// This is a plain POCO so Microsoft.AspNetCore.OpenApi can introspect it (and the types reachable only
/// through it — <see cref="AGUIToolCall"/>, <see cref="AGUIMessageRole"/>, …). The only non-trivial field,
/// <c>content</c> (string or multimodal array), is handled by a property-level converter on
/// <see cref="RawContent"/> rather than a type-level converter on the whole message.
/// </remarks>
public sealed class AGUIMessage
{
    /// <summary>
    /// Gets or sets the message identifier. Required by the AG-UI spec on every message variant.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the message role.
    /// </summary>
    [JsonPropertyName("role")]
    public AGUIMessageRole Role { get; set; }

    /// <summary>
    /// Gets or sets the wire representation of the <c>content</c> field (a JSON string or a multimodal
    /// array). Prefer <see cref="Content"/> / <see cref="ContentParts"/> to read or write content.
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AGUIMessageContent? RawContent { get; set; }

    /// <summary>
    /// Gets or sets the message content as plain text. When <see cref="ContentParts"/> is set, this is
    /// derived from the text content parts. Backed by <see cref="RawContent"/>.
    /// </summary>
    [JsonIgnore]
    public string? Content
    {
        get => RawContent?.Text;
        set
        {
            if (value is null)
            {
                if (RawContent is not null)
                {
                    RawContent.Text = null;
                    if (RawContent.Parts is null)
                    {
                        RawContent = null;
                    }
                }

                return;
            }

            (RawContent ??= new AGUIMessageContent()).Text = value;
        }
    }

    /// <summary>
    /// Gets or sets the multimodal content parts. When set, the content serializes as a JSON array.
    /// Backed by <see cref="RawContent"/>.
    /// </summary>
    [JsonIgnore]
    public IList<AGUIInputContent>? ContentParts
    {
        get => RawContent?.Parts;
        set
        {
            if (value is null)
            {
                if (RawContent is not null)
                {
                    RawContent.Parts = null;
                    if (RawContent.Text is null)
                    {
                        RawContent = null;
                    }
                }

                return;
            }

            (RawContent ??= new AGUIMessageContent()).Parts = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional name (for tool messages).
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the encrypted-value envelope per AG-UI spec — opaque payload providers may use
    /// for zero-data-retention round-trips. Optional on every message variant.
    /// </summary>
    [JsonPropertyName("encryptedValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EncryptedValue { get; set; }

    /// <summary>
    /// Gets or sets the tool calls made by the assistant.
    /// </summary>
    [JsonPropertyName("toolCalls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<AGUIToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the tool call ID this message is responding to.
    /// </summary>
    [JsonPropertyName("toolCallId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Gets or sets the tool-execution error message (tool-role only per AG-UI spec).
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}
