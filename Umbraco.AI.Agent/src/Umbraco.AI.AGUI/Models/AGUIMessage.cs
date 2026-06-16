using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Represents a message in the AG-UI protocol.
/// Supports both plain string content and multimodal content parts (AG-UI multimodal messages draft).
/// </summary>
[JsonConverter(typeof(AGUIMessageJsonConverter))]
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
    /// Gets or sets the message content as plain text.
    /// When <see cref="ContentParts"/> is set, this is derived from text content parts.
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the multimodal content parts.
    /// When set, the message contains mixed text and binary content (AG-UI multimodal messages draft).
    /// </summary>
    [JsonIgnore]
    public IList<AGUIInputContent>? ContentParts { get; set; }

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

/// <summary>
/// Custom JSON converter for <see cref="AGUIMessage"/> that handles the AG-UI multimodal messages draft.
/// The <c>content</c> field can be either a JSON string or a JSON array of <see cref="AGUIInputContent"/> parts.
/// </summary>
internal sealed class AGUIMessageJsonConverter : JsonConverter<AGUIMessage>
{
    public override AGUIMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token for AGUIMessage.");

        string? id = null;
        var role = default(AGUIMessageRole);
        string? content = null;
        IList<AGUIInputContent>? contentParts = null;
        string? name = null;
        string? encryptedValue = null;
        IEnumerable<AGUIToolCall>? toolCalls = null;
        string? toolCallId = null;
        string? error = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token.");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "id":
                    id = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;

                case "role":
                    role = JsonSerializer.Deserialize<AGUIMessageRole>(ref reader, options);
                    break;

                case "content":
                    ReadContent(ref reader, ref content, ref contentParts, options);
                    break;

                case "name":
                    name = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;

                case "encryptedValue":
                    encryptedValue = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;

                case "toolCalls":
                    toolCalls = reader.TokenType == JsonTokenType.Null
                        ? null
                        : JsonSerializer.Deserialize<List<AGUIToolCall>>(ref reader, options);
                    break;

                case "toolCallId":
                    toolCallId = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;

                case "error":
                    error = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;

                default:
                    // Skip unknown properties
                    reader.Skip();
                    break;
            }
        }

        if (id is null)
        {
            throw new JsonException("AGUIMessage 'id' is required by the AG-UI spec.");
        }

        return new AGUIMessage
        {
            Id = id,
            Role = role,
            Content = content,
            ContentParts = contentParts,
            Name = name,
            EncryptedValue = encryptedValue,
            ToolCalls = toolCalls,
            ToolCallId = toolCallId,
            Error = error,
        };
    }

    private static void ReadContent(
        ref Utf8JsonReader reader,
        ref string? content,
        ref IList<AGUIInputContent>? contentParts,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                // Plain string content (backward compatible)
                content = reader.GetString();
                break;

            case JsonTokenType.StartArray:
                // Multimodal content parts array
                var parts = JsonSerializer.Deserialize<List<AGUIInputContent>>(ref reader, options);
                contentParts = parts;
                // Derive text content from text parts for backward compatibility
                content = parts != null
                    ? string.Join("", parts.OfType<AGUITextInputContent>().Select(t => t.Text))
                    : null;
                break;

            case JsonTokenType.Null:
                content = null;
                break;

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} for 'content' property.");
        }
    }

    public override void Write(Utf8JsonWriter writer, AGUIMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("id", value.Id);

        writer.WritePropertyName("role");
        JsonSerializer.Serialize(writer, value.Role, options);

        // Write content as array when ContentParts is set, otherwise as string
        if (value.ContentParts != null)
        {
            writer.WritePropertyName("content");
            JsonSerializer.Serialize(writer, value.ContentParts, options);
        }
        else if (value.Content != null)
        {
            writer.WriteString("content", value.Content);
        }

        if (value.Name != null)
        {
            writer.WriteString("name", value.Name);
        }

        if (value.EncryptedValue != null)
        {
            writer.WriteString("encryptedValue", value.EncryptedValue);
        }

        if (value.ToolCalls != null)
        {
            writer.WritePropertyName("toolCalls");
            JsonSerializer.Serialize(writer, value.ToolCalls, options);
        }

        if (value.ToolCallId != null)
        {
            writer.WriteString("toolCallId", value.ToolCallId);
        }

        if (value.Error != null)
        {
            writer.WriteString("error", value.Error);
        }

        writer.WriteEndObject();
    }
}
