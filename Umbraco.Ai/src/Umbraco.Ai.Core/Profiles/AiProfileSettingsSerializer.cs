using System.Text.Json;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Serializer for capability-specific profile settings.
/// </summary>
internal static class AiProfileSettingsSerializer
{
    /// <summary>
    /// Serializes profile settings to JSON.
    /// </summary>
    public static string? Serialize(IAiProfileSettings? settings)
    {
        if (settings is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(settings, settings.GetType(), Constants.DefaultJsonSerializerOptions);
    }

    /// <summary>
    /// Deserializes profile settings from JSON based on capability type.
    /// </summary>
    public static IAiProfileSettings? Deserialize(AiCapability capability, string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return capability switch
        {
            AiCapability.Chat => JsonSerializer.Deserialize<AiChatProfileSettings>(json, Constants.DefaultJsonSerializerOptions),
            AiCapability.Embedding => JsonSerializer.Deserialize<AiEmbeddingProfileSettings>(json, Constants.DefaultJsonSerializerOptions),
            // Future: Add Media, Moderation cases when implemented
            _ => null
        };
    }
}
