using System.Text.Json;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Serializer for capability-specific profile settings.
/// </summary>
internal static class AIProfileSettingsSerializer
{
    /// <summary>
    /// Serializes profile settings to JSON.
    /// </summary>
    public static string? Serialize(IAIProfileSettings? settings)
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
    public static IAIProfileSettings? Deserialize(AICapability capability, string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return capability switch
        {
            AICapability.Chat => JsonSerializer.Deserialize<AIChatProfileSettings>(json, Constants.DefaultJsonSerializerOptions),
            AICapability.Embedding => JsonSerializer.Deserialize<AIEmbeddingProfileSettings>(json, Constants.DefaultJsonSerializerOptions),
            // Future: Add Media, Moderation cases when implemented
            _ => null
        };
    }
}
