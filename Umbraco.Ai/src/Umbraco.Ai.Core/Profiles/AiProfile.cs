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
    public Guid Id { get; internal set; }

    /// <summary>
    /// The alias of the AI profile.
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// The name of the AI profile.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The capability of the AI profile (e.g., Chat, Embedding, etc.).
    /// Cannot be changed after creation.
    /// </summary>
    public AiCapability Capability { get; init; } = AiCapability.Chat;

    /// <summary>
    /// The AI model reference associated with this profile.
    /// </summary>
    public AiModelRef Model { get; set; }

    /// <summary>
    /// The ID of the connection to use for this profile.
    /// Must reference a valid AiConnection.Id that matches the provider in Model.ProviderId.
    /// </summary>
    public required Guid ConnectionId { get; set; }

    /// <summary>
    /// Capability-specific settings. Type depends on <see cref="Capability"/> value.
    /// </summary>
    public IAiProfileSettings? Settings { get; set; }

    /// <summary>
    /// A list of tags associated with the AI profile for categorization and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
}
