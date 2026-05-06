using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Core.Serialization;

/// <summary>
/// JSON converter for <see cref="double"/> properties bound to the Umbraco backoffice slider
/// (<c>Umb.PropertyEditorUi.Slider</c>), which serialises its value as <c>{ "from": n, "to": n }</c>.
/// Reads the <c>from</c> field when the value is an object, otherwise falls back to standard
/// number / string-number parsing.
/// </summary>
public sealed class SliderDoubleJsonConverter : JsonConverter<double>
{
    /// <inheritdoc />
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetDouble();

            case JsonTokenType.String:
                var s = reader.GetString();
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }
                throw new JsonException($"Cannot convert string '{s}' to double.");

            case JsonTokenType.StartObject:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    if (doc.RootElement.TryGetProperty("from", out var fromElement))
                    {
                        return fromElement.ValueKind switch
                        {
                            JsonValueKind.Number => fromElement.GetDouble(),
                            JsonValueKind.String when double.TryParse(
                                fromElement.GetString(),
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out var fromString) => fromString,
                            _ => throw new JsonException("Slider 'from' value is not a number.")
                        };
                    }
                    throw new JsonException("Object value has no 'from' property to convert to double.");
                }

            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when reading double.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
