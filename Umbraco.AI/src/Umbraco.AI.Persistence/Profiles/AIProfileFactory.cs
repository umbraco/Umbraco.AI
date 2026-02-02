using System.Text.Json;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Persistence.Profiles;

/// <summary>
/// Factory for mapping between <see cref="AIProfile"/> domain models and <see cref="AIProfileEntity"/> database entities.
/// </summary>
internal static class AIProfileFactory
{
    /// <summary>
    /// Creates an <see cref="AIProfile"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AIProfile BuildDomain(AIProfileEntity entity)
    {
        IReadOnlyList<string> tags = Array.Empty<string>();
        if (!string.IsNullOrEmpty(entity.Tags))
        {
            tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var capability = (AICapability)entity.Capability;

        return new AIProfile
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Capability = capability,
            Model = new AIModelRef(entity.ProviderId, entity.ModelId),
            ConnectionId = entity.ConnectionId,
            Settings = AIProfileSettingsSerializer.Deserialize(capability, entity.Settings),
            Tags = tags,
            Version = entity.Version,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId
        };
    }

    /// <summary>
    /// Creates an <see cref="AIProfileEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="profile">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AIProfileEntity BuildEntity(AIProfile profile)
    {
        return new AIProfileEntity
        {
            Id = profile.Id,
            Alias = profile.Alias,
            Name = profile.Name,
            Capability = (int)profile.Capability,
            ProviderId = profile.Model.ProviderId,
            ModelId = profile.Model.ModelId,
            ConnectionId = profile.ConnectionId,
            Settings = AIProfileSettingsSerializer.Serialize(profile.Settings),
            Tags = profile.Tags.Count > 0 ? string.Join(',', profile.Tags) : null,
            Version = profile.Version,
            DateCreated = profile.DateCreated,
            DateModified = profile.DateModified,
            CreatedByUserId = profile.CreatedByUserId,
            ModifiedByUserId = profile.ModifiedByUserId
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AIProfileEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="profile">The domain model with updated values.</param>
    public static void UpdateEntity(AIProfileEntity entity, AIProfile profile)
    {
        entity.Alias = profile.Alias;
        entity.Name = profile.Name;
        entity.Capability = (int)profile.Capability;
        entity.ProviderId = profile.Model.ProviderId;
        entity.ModelId = profile.Model.ModelId;
        entity.ConnectionId = profile.ConnectionId;
        entity.Settings = AIProfileSettingsSerializer.Serialize(profile.Settings);
        entity.Tags = profile.Tags.Count > 0 ? string.Join(',', profile.Tags) : null;
        entity.Version = profile.Version;
        entity.DateModified = profile.DateModified;
        entity.ModifiedByUserId = profile.ModifiedByUserId;
        // DateCreated and CreatedByUserId are intentionally not updated
    }
}
