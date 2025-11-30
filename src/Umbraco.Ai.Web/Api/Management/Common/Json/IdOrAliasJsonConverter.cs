using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Ai.Web.Api.Management.Common.Models;

namespace Umbraco.Ai.Web.Api.Management.Common.Json;

/// <summary>
/// JSON converter for <see cref="IdOrAlias"/> that deserializes from a string value.
/// </summary>
internal class IdOrAliasJsonConverter : JsonConverter<IdOrAlias>
{
    /// <inheritdoc />
    public override IdOrAlias? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (value != null)
            {
                return IdOrAlias.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IdOrAlias value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
