using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Core.Serialization;

/// <summary>
/// JSON converter for <see cref="string"/> properties bound to the Umbraco backoffice dropdown
/// (<c>Umb.PropertyEditorUi.Dropdown</c>), which serialises its value as an array of strings even when
/// configured as a single select (<c>multiple: false</c>) — so a field the model declares as a string
/// arrives as <c>["low"]</c>. Reads the first element when the value is an array, otherwise falls back to
/// standard string reading.
/// </summary>
/// <remarks>
/// An empty array reads as <c>null</c>, which is how the dropdown represents its cleared state. Extra
/// elements are ignored rather than rejected: cardinality is the field's own configuration, and a
/// multi-select bound to a string property is a declaration mistake that should not fail a request at
/// runtime.
/// </remarks>
public sealed class DropdownStringJsonConverter : JsonConverter<string?>
{
    /// <inheritdoc />
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.Null:
                return null;

            case JsonTokenType.StartArray:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        return element.ValueKind switch
                        {
                            JsonValueKind.String => element.GetString(),
                            JsonValueKind.Null => null,
                            _ => throw new JsonException(
                                $"Dropdown array element is {element.ValueKind}, not a string.")
                        };
                    }

                    // Empty array — the dropdown's cleared state.
                    return null;
                }

            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading string.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }
}
