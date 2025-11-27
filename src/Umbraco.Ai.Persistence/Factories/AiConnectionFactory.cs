using System.Text.Json;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Persistence.Entities;

namespace Umbraco.Ai.Persistence.Factories;

/// <summary>
/// Factory for mapping between <see cref="AiConnection"/> domain models and <see cref="AiConnectionEntity"/> database entities.
/// </summary>
internal static class AiConnectionFactory
{
    /// <summary>
    /// Creates an <see cref="AiConnection"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiConnection BuildDomain(AiConnectionEntity entity)
    {
        object? settings = null;
        if (!string.IsNullOrEmpty(entity.SettingsJson))
        {
            // Settings are stored as JSON, deserialize to dynamic object
            // The actual typed deserialization happens at the service layer
            settings = JsonSerializer.Deserialize<JsonElement>(entity.SettingsJson);
        }

        return new AiConnection
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            ProviderId = entity.ProviderId,
            Settings = settings,
            IsActive = entity.IsActive,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified
        };
    }

    /// <summary>
    /// Creates an <see cref="AiConnectionEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="connection">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiConnectionEntity BuildEntity(AiConnection connection)
    {
        return new AiConnectionEntity
        {
            Id = connection.Id,
            Alias = connection.Alias,
            Name = connection.Name,
            ProviderId = connection.ProviderId,
            SettingsJson = connection.Settings is null ? null : JsonSerializer.Serialize(connection.Settings),
            IsActive = connection.IsActive,
            DateCreated = connection.DateCreated,
            DateModified = connection.DateModified
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AiConnectionEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="connection">The domain model with updated values.</param>
    public static void UpdateEntity(AiConnectionEntity entity, AiConnection connection)
    {
        entity.Alias = connection.Alias;
        entity.Name = connection.Name;
        entity.ProviderId = connection.ProviderId;
        entity.SettingsJson = connection.Settings is null ? null : JsonSerializer.Serialize(connection.Settings);
        entity.IsActive = connection.IsActive;
        entity.DateModified = connection.DateModified;
    }
}
