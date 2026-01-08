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

    /// <summary>
    /// Property editor constants for Umbraco.Ai.
    /// </summary>
    internal static class PropertyEditors
    {
        /// <summary>
        /// Property editor aliases.
        /// </summary>
        internal static class Aliases
        {
            /// <summary>
            /// Alias for the AI Context Picker property editor.
            /// </summary>
            public const string ContextPicker = "Uai.ContextPicker";
        }
    }
}