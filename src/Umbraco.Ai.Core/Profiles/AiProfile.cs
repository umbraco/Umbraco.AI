using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Defines a profile for AI model usage, including model reference, capabilities, and configuration settings.
/// </summary>
public sealed class AiProfile
{
    /// <summary>
    /// The unique identifier of the AI profile.
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The alias of the AI profile.
    /// </summary>
    public required string Alias { get; init; }
    
    /// <summary>
    /// The name of the AI profile.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The capability of the AI profile (e.g., Text, Image, etc.).
    /// </summary>
    public AiCapability Capability { get; init; } = AiCapability.Chat;
    
    /// <summary>
    /// The AI model reference associated with this profile.
    /// </summary>
    public AiModelRef Model { get; init; }

    /// <summary>
    /// The ID of the connection to use for this profile.
    /// Must reference a valid AiConnection.Id that matches the provider in Model.ProviderId.
    /// </summary>
    public required Guid ConnectionId { get; init; }

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
    /// A list of tags associated with the AI profile for categorization and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>(); 
}