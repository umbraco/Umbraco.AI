using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Core.Serialization;

/// <summary>
/// JSON converter that serializes a <see cref="Type"/> to its assembly-qualified name
/// and deserializes it back to a <see cref="Type"/>.
/// </summary>
public class JsonStringTypeConverter : JsonConverter<Type>
{
    /// <inheritdoc />
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var typeName = reader.GetString();
        return string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.FullName}, {value.Assembly.GetName().Name}");
    }
}