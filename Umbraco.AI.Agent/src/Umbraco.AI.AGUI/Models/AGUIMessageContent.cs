using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Serialization shape of an <see cref="AGUIMessage"/>'s <c>content</c> field, which per the AG-UI
/// multimodal messages draft is either a plain JSON string or a JSON array of
/// <see cref="AGUIInputContent"/> parts.
/// </summary>
/// <remarks>
/// This type carries the string-or-array handling in a <em>property-level</em> converter, so that
/// <see cref="AGUIMessage"/> itself stays a plain POCO that Microsoft.AspNetCore.OpenApi can introspect
/// (otherwise a type-level converter makes the whole message — and every type only reachable through it —
/// opaque, dropping their schemas). Prefer <see cref="AGUIMessage.Content"/> and
/// <see cref="AGUIMessage.ContentParts"/> to read or write content; this type is the wire representation.
/// </remarks>
[JsonConverter(typeof(AGUIMessageContentJsonConverter))]
public sealed class AGUIMessageContent
{
    /// <summary>
    /// Gets or sets the plain-text content. When the content is a multimodal array, this is derived
    /// from the text parts for backward compatibility.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the multimodal content parts. When set, the content serializes as a JSON array.
    /// </summary>
    public IList<AGUIInputContent>? Parts { get; set; }

    /// <summary>
    /// Derives a plain-text representation by concatenating the text parts.
    /// </summary>
    internal static string DeriveText(IList<AGUIInputContent> parts)
        => string.Join("", parts.OfType<AGUITextInputContent>().Select(t => t.Text));
}

/// <summary>
/// Reads/writes <see cref="AGUIMessageContent"/> as the AG-UI <c>content</c> field — a JSON string or a
/// JSON array of <see cref="AGUIInputContent"/> parts.
/// </summary>
internal sealed class AGUIMessageContentJsonConverter : JsonConverter<AGUIMessageContent>
{
    public override AGUIMessageContent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                return new AGUIMessageContent { Text = reader.GetString() };

            case JsonTokenType.StartArray:
                var parts = JsonSerializer.Deserialize<List<AGUIInputContent>>(ref reader, options);
                return new AGUIMessageContent
                {
                    Parts = parts,
                    // Derive text from text parts for backward compatibility with string-only consumers.
                    Text = parts is not null ? AGUIMessageContent.DeriveText(parts) : null,
                };

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} for 'content'.");
        }
    }

    public override void Write(Utf8JsonWriter writer, AGUIMessageContent value, JsonSerializerOptions options)
    {
        // Write the array form when parts are present, otherwise the string form (matching the AG-UI spec).
        if (value.Parts is not null)
        {
            JsonSerializer.Serialize(writer, value.Parts, options);
        }
        else if (value.Text is not null)
        {
            writer.WriteStringValue(value.Text);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
