using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Web.Api.Common.Json;

/// <summary>
/// JSON converter that writes a string-keyed dictionary with its keys exactly as stored, ignoring
/// <see cref="JsonSerializerOptions.DictionaryKeyPolicy"/>.
/// </summary>
/// <remarks>
/// The Management API camel-cases dictionary keys. That is right for keys that are C# names, but wrong
/// for keys that are caller data, such as Decision option keys, which are opaque and compared exactly.
/// Apply this converter to those properties only. The OpenAPI schema is unaffected: the property still
/// documents as a plain map.
/// </remarks>
/// <typeparam name="TValue">The dictionary value type.</typeparam>
internal sealed class ExactKeyDictionaryJsonConverter<TValue> : JsonConverter<IReadOnlyDictionary<string, TValue>>
{
    /// <inheritdoc />
    public override IReadOnlyDictionary<string, TValue>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
        // The key policy only applies when writing, so the default read already keeps keys as sent.
        => JsonSerializer.Deserialize<Dictionary<string, TValue>>(ref reader, options);

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, TValue> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (key, item) in value)
        {
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, item, options);
        }

        writer.WriteEndObject();
    }
}
