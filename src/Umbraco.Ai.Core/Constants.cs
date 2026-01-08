using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Ai.Core.Serialization;

namespace Umbraco.Ai.Core;

internal static class Constants
{
    internal static JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(),
            new JsonStringTypeConverter()
        }
    };
}