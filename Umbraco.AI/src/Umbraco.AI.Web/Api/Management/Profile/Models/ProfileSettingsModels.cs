using System.Text.Json.Serialization;

namespace Umbraco.AI.Web.Api.Management.Profile.Models;

/// <summary>
/// Base class for capability-specific profile settings in the API.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ChatProfileSettingsModel), "chat")]
[JsonDerivedType(typeof(EmbeddingProfileSettingsModel), "embedding")]
public abstract class ProfileSettingsModel { }

/// <summary>
/// Settings model for Chat capability profiles.
/// </summary>
public class ChatProfileSettingsModel : ProfileSettingsModel
{
    /// <summary>
    /// The temperature setting for the AI model, influencing randomness in responses.
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// The maximum number of tokens the AI model can generate in a single response.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// The system prompt template to be used with the AI model.
    /// </summary>
    public string? SystemPromptTemplate { get; init; }

    /// <summary>
    /// Context IDs assigned to this profile for AI context injection.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; init; } = [];
}

/// <summary>
/// Settings model for Embedding capability profiles.
/// </summary>
public class EmbeddingProfileSettingsModel : ProfileSettingsModel { }
