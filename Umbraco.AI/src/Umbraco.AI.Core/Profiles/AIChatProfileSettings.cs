namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Settings specific to Chat capability profiles.
/// </summary> 
public sealed class AIChatProfileSettings : IAIProfileSettings
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
    /// These contexts provide brand voice, guidelines, and reference materials for AI operations.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; init; } = [];
}
