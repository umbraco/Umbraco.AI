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
    /// Capability-specific settings. Type depends on <see cref="Capability"/> value.
    /// </summary>
    public IAiProfileSettings? Settings { get; init; }

    /// <summary>
    /// A list of tags associated with the AI profile for categorization and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the settings as <see cref="AiChatProfileSettings"/> if this is a Chat profile.
    /// </summary>
    public AiChatProfileSettings? ChatSettings => Settings as AiChatProfileSettings;

    /// <summary>
    /// Gets the settings as <see cref="AiEmbeddingProfileSettings"/> if this is an Embedding profile.
    /// </summary>
    public AiEmbeddingProfileSettings? EmbeddingSettings => Settings as AiEmbeddingProfileSettings; 
}