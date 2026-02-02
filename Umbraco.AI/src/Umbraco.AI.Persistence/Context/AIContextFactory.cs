using System.Text.Json;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Contexts;

namespace Umbraco.AI.Persistence.Context;

/// <summary>
/// Factory for mapping between <see cref="AIContext"/> domain models and <see cref="AIContextEntity"/> database entities.
/// </summary>
internal static class AIContextFactory
{
    /// <summary>
    /// Creates an <see cref="AIContext"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AIContext BuildDomain(AIContextEntity entity)
    {
        return new AIContext
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId,
            Version = entity.Version,
            Resources = entity.Resources
                .OrderBy(r => r.SortOrder)
                .Select(BuildResourceDomain)
                .ToList()
        };
    }

    /// <summary>
    /// Creates an <see cref="AIContextResource"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AIContextResource BuildResourceDomain(AIContextResourceEntity entity)
    {
        object? data = null;
        if (!string.IsNullOrEmpty(entity.Data))
        {
            // Data is stored as JSON, deserialize to dynamic object
            // The actual typed deserialization happens at the service layer
            data = JsonSerializer.Deserialize<JsonElement>(entity.Data, Constants.DefaultJsonSerializerOptions);
        }

        return new AIContextResource
        {
            Id = entity.Id,
            ResourceTypeId = entity.ResourceTypeId,
            Name = entity.Name,
            Description = entity.Description,
            SortOrder = entity.SortOrder,
            Data = data,
            InjectionMode = (AIContextResourceInjectionMode)entity.InjectionMode
        };
    }

    /// <summary>
    /// Creates an <see cref="AIContextEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="context">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AIContextEntity BuildEntity(AIContext context)
    {
        return new AIContextEntity
        {
            Id = context.Id,
            Alias = context.Alias,
            Name = context.Name,
            DateCreated = context.DateCreated,
            DateModified = context.DateModified,
            CreatedByUserId = context.CreatedByUserId,
            ModifiedByUserId = context.ModifiedByUserId,
            Version = context.Version,
            Resources = context.Resources
                .Select(r => BuildResourceEntity(r, context.Id))
                .ToList()
        };
    }

    /// <summary>
    /// Creates an <see cref="AIContextResourceEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="resource">The domain model.</param>
    /// <param name="contextId">The parent context ID.</param>
    /// <returns>The database entity.</returns>
    public static AIContextResourceEntity BuildResourceEntity(AIContextResource resource, Guid contextId)
    {
        return new AIContextResourceEntity
        {
            Id = resource.Id,
            ContextId = contextId,
            ResourceTypeId = resource.ResourceTypeId,
            Name = resource.Name,
            Description = resource.Description,
            SortOrder = resource.SortOrder,
            Data = resource.Data is null ? string.Empty : JsonSerializer.Serialize(resource.Data, Constants.DefaultJsonSerializerOptions),
            InjectionMode = (int)resource.InjectionMode
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AIContextEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="context">The domain model with updated values.</param>
    public static void UpdateEntity(AIContextEntity entity, AIContext context)
    {
        entity.Alias = context.Alias;
        entity.Name = context.Name;
        entity.DateModified = context.DateModified;
        entity.ModifiedByUserId = context.ModifiedByUserId;
        entity.Version = context.Version;
        // Resources are handled separately in the repository
        // DateCreated and CreatedByUserId are intentionally not updated
    }

    /// <summary>
    /// Updates an existing <see cref="AIContextResourceEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="resource">The domain model with updated values.</param>
    public static void UpdateResourceEntity(AIContextResourceEntity entity, AIContextResource resource)
    {
        entity.ResourceTypeId = resource.ResourceTypeId;
        entity.Name = resource.Name;
        entity.Description = resource.Description;
        entity.SortOrder = resource.SortOrder;
        entity.Data = resource.Data is null ? string.Empty : JsonSerializer.Serialize(resource.Data, Constants.DefaultJsonSerializerOptions);
        entity.InjectionMode = (int)resource.InjectionMode;
    }
}
