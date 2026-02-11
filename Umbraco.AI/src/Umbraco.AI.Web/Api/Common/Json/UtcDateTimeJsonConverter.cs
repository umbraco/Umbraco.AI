using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Web.Api.Common.Json;

/// <summary>
/// JSON converter that ensures DateTime values are serialized as UTC with explicit timezone indicator.
/// This prevents timezone interpretation issues in JavaScript clients by always including the "Z" suffix.
/// </summary>
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTimeString = reader.GetString();
        if (dateTimeString is null)
        {
            return default;
        }

        // Parse the DateTime and convert to UTC if it has timezone info
        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            return dateTime.ToUniversalTime();
        }

        throw new JsonException($"Unable to parse '{dateTimeString}' as DateTime.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always convert to UTC and format with explicit "Z" suffix
        var utcDateTime = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

/// <summary>
/// JSON converter that ensures nullable DateTime values are serialized as UTC with explicit timezone indicator.
/// </summary>
public class UtcNullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTimeString = reader.GetString();
        if (string.IsNullOrEmpty(dateTimeString))
        {
            return null;
        }

        // Parse the DateTime and convert to UTC if it has timezone info
        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            return dateTime.ToUniversalTime();
        }

        throw new JsonException($"Unable to parse '{dateTimeString}' as DateTime.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // Always convert to UTC and format with explicit "Z" suffix
        var utcDateTime = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
