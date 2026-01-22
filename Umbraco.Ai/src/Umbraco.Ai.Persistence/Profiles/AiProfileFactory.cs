using System.Text.Json;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Persistence.Profiles;

/// <summary>
/// Factory for mapping between <see cref="AiProfile"/> domain models and <see cref="AiProfileEntity"/> database entities.
/// </summary>
internal static class AiProfileFactory
{
    /// <summary>
    /// Creates an <see cref="AiProfile"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiProfile BuildDomain(AiProfileEntity entity)
    {
        IReadOnlyList<string> tags = Array.Empty<string>();
        if (!string.IsNullOrEmpty(entity.Tags))
        {
            tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var capability = (AiCapability)entity.Capability;

        return new AiProfile
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Capability = capability,
            Model = new AiModelRef(entity.ProviderId, entity.ModelId),
            ConnectionId = entity.ConnectionId,
            Settings = AiProfileSettingsSerializer.Deserialize(capability, entity.Settings),
            Tags = tags,
            Version = entity.Version,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId
        };
    }

    /// <summary>
    /// Creates an <see cref="AiProfileEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="profile">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiProfileEntity BuildEntity(AiProfile profile)
    {
        return new AiProfileEntity
        {
            Id = profile.Id,
            Alias = profile.Alias,
            Name = profile.Name,
            Capability = (int)profile.Capability,
            ProviderId = profile.Model.ProviderId,
            ModelId = profile.Model.ModelId,
            ConnectionId = profile.ConnectionId,
            Settings = AiProfileSettingsSerializer.Serialize(profile.Settings),
            Tags = profile.Tags.Count > 0 ? string.Join(',', profile.Tags) : null,
            Version = profile.Version,
            DateCreated = profile.DateCreated,
            DateModified = profile.DateModified,
            CreatedByUserId = profile.CreatedByUserId,
            ModifiedByUserId = profile.ModifiedByUserId
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AiProfileEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="profile">The domain model with updated values.</param>
    public static void UpdateEntity(AiProfileEntity entity, AiProfile profile)
    {
        entity.Alias = profile.Alias;
        entity.Name = profile.Name;
        entity.Capability = (int)profile.Capability;
        entity.ProviderId = profile.Model.ProviderId;
        entity.ModelId = profile.Model.ModelId;
        entity.ConnectionId = profile.ConnectionId;
        entity.Settings = AiProfileSettingsSerializer.Serialize(profile.Settings);
        entity.Tags = profile.Tags.Count > 0 ? string.Join(',', profile.Tags) : null;
        entity.Version = profile.Version;
        entity.DateModified = profile.DateModified;
        entity.ModifiedByUserId = profile.ModifiedByUserId;
        // DateCreated and CreatedByUserId are intentionally not updated
    }
}
