namespace Umbraco.Ai.Persistence.Entities;

/// <summary>
/// EF Core entity representing an AI profile configuration.
/// </summary>
public class AiProfileEntity
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique alias for the profile (used for lookup).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The capability type (Chat=0, Embedding=1, Media=2, Moderation=3).
    /// </summary>
    public int Capability { get; set; }

    /// <summary>
    /// The ID of the AI provider.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the model within the provider.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the connection.
    /// </summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Temperature setting for the model (0.0-2.0 typically).
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens for model responses.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// System prompt template.
    /// </summary>
    public string? SystemPromptTemplate { get; set; }

    /// <summary>
    /// JSON-serialized array of tags.
    /// </summary>
    public string? TagsJson { get; set; }
}
