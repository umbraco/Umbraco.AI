using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.Core;

/// <summary>
/// Constants for Umbraco.AI.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Default JSON serializer options for Umbraco.AI. 
    /// </summary>
    public static JsonSerializerOptions DefaultJsonSerializerOptions = new()
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
    /// Property editor constants for Umbraco.AI.
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
    public static class ContextKeys
    {
        /// <summary>
        /// Key for profile ID in metadata collections.
        /// </summary>
        public const string ProfileId = "Umbraco.AI.ProfileId";
    
        /// <summary>
        /// Key for profile alias in metadata collections.
        /// </summary>
        public const string ProfileAlias = "Umbraco.AI.ProfileAlias";
    
        /// <summary>
        /// Key for provider ID in metadata collections.
        /// </summary>
        public const string ProviderId = "Umbraco.AI.ProviderId";
    
        /// <summary>
        /// Key for model ID in metadata collections.
        /// </summary>
        public const string ModelId = "Umbraco.AI.ModelId";
    
        /// <summary>
        /// Key for feature ID in metadata collections.
        /// </summary>
        public const string FeatureId = "Umbraco.AI.FeatureId";
    
        /// <summary>
        /// Key for feature alias in metadata collections.
        /// </summary>
        public const string FeatureAlias = "Umbraco.AI.FeatureAlias";
    
        /// <summary>
        /// Key for feature type in metadata collections.
        /// </summary>
        public const string FeatureType = "Umbraco.AI.FeatureType";

        /// <summary>
        /// Key for profile version in metadata collections.
        /// </summary>
        public const string ProfileVersion = "Umbraco.AI.ProfileVersion";

        /// <summary>
        /// Key for feature version in metadata collections.
        /// </summary>
        public const string FeatureVersion = "Umbraco.AI.FeatureVersion";

        /// <summary>
        /// Key for entity ID in metadata collections.
        /// </summary>
        public const string EntityId = "Umbraco.AI.EntityId";
    
        /// <summary>
        /// Key for entity type in metadata collections.
        /// </summary>
        public const string EntityType = "Umbraco.AI.EntityType";

        /// <summary>
        /// Key for section pathname in metadata collections.
        /// </summary>
        /// <example>"content", "media", "settings"</example>
        public const string Section = "Umbraco.AI.Section";

        /// <summary>
        /// Key for log keys in metadata collections.
        /// </summary>
        public const string LogKeys = "Umbraco.AI.LogKeys";
        
        /// <summary>
        /// Key for <see cref="EntityAdapter.AISerializedEntity"/> data.
        /// </summary>
        public const string SerializedEntity = "Umbraco.AI.SerializedEntity";

        /// <summary>
        /// Key for the parent entity unique identifier (as Guid).
        /// Set when creating a new entity under a parent.
        /// </summary>
        public const string ParentEntityId = "Umbraco.AI.ParentEntityId";
    }
}