using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Defines a profile for AI model usage, including model reference, capabilities, and configuration settings.
/// </summary>
public sealed class AiProfile : IAiVersionableEntity
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

    /// <summary>
    /// The current version of the profile.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    public int Version { get; internal set; } = 1;

    /// <summary>
    /// The date and time when the profile was created.
    /// </summary>
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time when the profile was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The key (GUID) of the user who created this profile.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this profile.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }
}
