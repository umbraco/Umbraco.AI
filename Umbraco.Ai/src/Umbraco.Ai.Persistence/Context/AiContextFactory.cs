using System.Text.Json;
using Umbraco.Ai.Core;
using Umbraco.Ai.Core.Contexts;

namespace Umbraco.Ai.Persistence.Context;

/// <summary>
/// Factory for mapping between <see cref="AiContext"/> domain models and <see cref="AiContextEntity"/> database entities.
/// </summary>
internal static class AiContextFactory
{
    /// <summary>
    /// Creates an <see cref="AiContext"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiContext BuildDomain(AiContextEntity entity)
    {
        return new AiContext
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
    /// Creates an <see cref="AiContextResource"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiContextResource BuildResourceDomain(AiContextResourceEntity entity)
    {
        object? data = null;
        if (!string.IsNullOrEmpty(entity.Data))
        {
            // Data is stored as JSON, deserialize to dynamic object
            // The actual typed deserialization happens at the service layer
            data = JsonSerializer.Deserialize<JsonElement>(entity.Data, Constants.DefaultJsonSerializerOptions);
        }

        return new AiContextResource
        {
            Id = entity.Id,
            ResourceTypeId = entity.ResourceTypeId,
            Name = entity.Name,
            Description = entity.Description,
            SortOrder = entity.SortOrder,
            Data = data,
            InjectionMode = (AiContextResourceInjectionMode)entity.InjectionMode
        };
    }

    /// <summary>
    /// Creates an <see cref="AiContextEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="context">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiContextEntity BuildEntity(AiContext context)
    {
        return new AiContextEntity
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
    /// Creates an <see cref="AiContextResourceEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="resource">The domain model.</param>
    /// <param name="contextId">The parent context ID.</param>
    /// <returns>The database entity.</returns>
    public static AiContextResourceEntity BuildResourceEntity(AiContextResource resource, Guid contextId)
    {
        return new AiContextResourceEntity
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
    /// Updates an existing <see cref="AiContextEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="context">The domain model with updated values.</param>
    public static void UpdateEntity(AiContextEntity entity, AiContext context)
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
    /// Updates an existing <see cref="AiContextResourceEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="resource">The domain model with updated values.</param>
    public static void UpdateResourceEntity(AiContextResourceEntity entity, AiContextResource resource)
    {
        entity.ResourceTypeId = resource.ResourceTypeId;
        entity.Name = resource.Name;
        entity.Description = resource.Description;
        entity.SortOrder = resource.SortOrder;
        entity.Data = resource.Data is null ? string.Empty : JsonSerializer.Serialize(resource.Data, Constants.DefaultJsonSerializerOptions);
        entity.InjectionMode = (int)resource.InjectionMode;
    }

    /// <summary>
    /// Creates a JSON snapshot of a context for version history storage.
    /// </summary>
    /// <param name="context">The context to snapshot.</param>
    /// <returns>JSON string representing the context state.</returns>
    public static string CreateSnapshot(AiContext context)
    {
        var snapshot = new
        {
            context.Id,
            context.Alias,
            context.Name,
            context.DateCreated,
            context.DateModified,
            context.CreatedByUserId,
            context.ModifiedByUserId,
            context.Version,
            Resources = context.Resources.Select(r => new
            {
                r.Id,
                r.ResourceTypeId,
                r.Name,
                r.Description,
                r.SortOrder,
                Data = r.Data is null ? null : JsonSerializer.Serialize(r.Data, Constants.DefaultJsonSerializerOptions),
                InjectionMode = (int)r.InjectionMode
            }).ToList()
        };

        return JsonSerializer.Serialize(snapshot, Constants.DefaultJsonSerializerOptions);
    }

    /// <summary>
    /// Creates a domain model from a JSON snapshot.
    /// </summary>
    /// <param name="json">The JSON snapshot.</param>
    /// <returns>The context domain model, or null if parsing fails.</returns>
    public static AiContext? BuildDomainFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var resources = new List<AiContextResource>();
            if (root.TryGetProperty("resources", out var resourcesElement) &&
                resourcesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var resourceElement in resourcesElement.EnumerateArray())
                {
                    object? data = null;
                    if (resourceElement.TryGetProperty("data", out var dataElement) &&
                        dataElement.ValueKind == JsonValueKind.String)
                    {
                        var dataJson = dataElement.GetString();
                        if (!string.IsNullOrEmpty(dataJson))
                        {
                            data = JsonSerializer.Deserialize<JsonElement>(dataJson, Constants.DefaultJsonSerializerOptions);
                        }
                    }

                    resources.Add(new AiContextResource
                    {
                        Id = resourceElement.GetProperty("id").GetGuid(),
                        ResourceTypeId = resourceElement.GetProperty("resourceTypeId").GetString()!,
                        Name = resourceElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                            ? nameEl.GetString() : null,
                        Description = resourceElement.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                            ? descEl.GetString() : null,
                        SortOrder = resourceElement.GetProperty("sortOrder").GetInt32(),
                        Data = data,
                        InjectionMode = (AiContextResourceInjectionMode)resourceElement.GetProperty("injectionMode").GetInt32()
                    });
                }
            }

            return new AiContext
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                DateCreated = root.GetProperty("dateCreated").GetDateTime(),
                DateModified = root.GetProperty("dateModified").GetDateTime(),
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null
                    ? cbu.GetInt32() : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null
                    ? mbu.GetInt32() : null,
                Version = root.GetProperty("version").GetInt32(),
                Resources = resources
            };
        }
        catch
        {
            return null;
        }
    }
}
