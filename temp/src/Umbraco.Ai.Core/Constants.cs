using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Ai.Core.Serialization;

namespace Umbraco.Ai.Core;

/// <summary>
/// Constants for Umbraco.Ai.
/// </summary>
public static class Constants
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
    
    /// <summary>
    /// Keys for metadata in metadata collections.
    /// </summary>
    public static class MetadataKeys
    {
        /// <summary>
        /// Key for profile ID in metadata collections.
        /// </summary>
        public const string ProfileId = "Umbraco.Ai.ProfileId";
    
        /// <summary>
        /// Key for profile alias in metadata collections.
        /// </summary>
        public const string ProfileAlias = "Umbraco.Ai.ProfileAlias";
    
        /// <summary>
        /// Key for provider ID in metadata collections.
        /// </summary>
        public const string ProviderId = "Umbraco.Ai.ProviderId";
    
        /// <summary>
        /// Key for model ID in metadata collections.
        /// </summary>
        public const string ModelId = "Umbraco.Ai.ModelId";
    
        /// <summary>
        /// Key for feature ID in metadata collections.
        /// </summary>
        public const string FeatureId = "Umbraco.Ai.FeatureId";
    
        /// <summary>
        /// Key for feature alias in metadata collections.
        /// </summary>
        public const string FeatureAlias = "Umbraco.Ai.FeatureAlias";
    
        /// <summary>
        /// Key for feature type in metadata collections.
        /// </summary>
        public const string FeatureType = "Umbraco.Ai.FeatureType";
    
        /// <summary>
        /// Key for entity ID in metadata collections.
        /// </summary>
        public const string EntityId = "Umbraco.Ai.EntityId";
    
        /// <summary>
        /// Key for entity type in metadata collections.
        /// </summary>
        public const string EntityType = "Umbraco.Ai.EntityType";
    
        /// <summary>
        /// Key for log keys in metadata collections.
        /// </summary>
        public const string LogKeys = "Umbraco.Ai.LogKeys";
    }
}