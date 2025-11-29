using System.Text.Json;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Persistence.Profiles;

/// <summary>
/// Serializer for capability-specific profile settings.
/// </summary>
internal static class AiProfileSettingsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes profile settings to JSON.
    /// </summary>
    public static string? Serialize(IAiProfileSettings? settings)
    {
        if (settings is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(settings, settings.GetType(), JsonOptions);
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
            AiCapability.Chat => JsonSerializer.Deserialize<AiChatProfileSettings>(json, JsonOptions),
            AiCapability.Embedding => JsonSerializer.Deserialize<AiEmbeddingProfileSettings>(json, JsonOptions),
            // Future: Add Media, Moderation cases when implemented
            _ => null
        };
    }
}
