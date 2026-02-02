using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Web.Api.Common.Json;

/// <summary>
/// JSON converter for <see cref="TraceIdentifier"/> that deserializes from a string value.
/// </summary>
internal class TraceIdentifierJsonConverter : JsonConverter<TraceIdentifier>
{
    /// <inheritdoc />
    public override TraceIdentifier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (value != null)
            {
                return TraceIdentifier.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TraceIdentifier value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
