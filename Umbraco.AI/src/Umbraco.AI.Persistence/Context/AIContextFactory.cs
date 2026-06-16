using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Persistence.Context;

/// <summary>
/// Factory for mapping between <see cref="AIContext"/> domain models and <see cref="AIContextEntity"/> database entities.
/// Handles encryption/decryption of sensitive resource settings during the mapping process.
/// </summary>
internal sealed class AIContextFactory : IAIContextFactory
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AIContextResourceTypeCollection _resourceTypes;

    public AIContextFactory(
        IAIEditableModelSerializer serializer,
        AIContextResourceTypeCollection resourceTypes)
    {
        _serializer = serializer;
        _resourceTypes = resourceTypes;
    }

    /// <inheritdoc />
    public AIContext BuildDomain(AIContextEntity entity)
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
    private AIContextResource BuildResourceDomain(AIContextResourceEntity entity)
    {
        // Settings are stored as JSON with sensitive fields encrypted.
        // The serializer decrypts any encrypted values; typed deserialization (and
        // configuration variable resolution) happens later at the service/resource-type layer.
        object? settings = _serializer.Deserialize(entity.Settings);

        return new AIContextResource
        {
            Id = entity.Id,
            ResourceTypeId = entity.ResourceTypeId,
            Name = entity.Name,
            Description = entity.Description,
            SortOrder = entity.SortOrder,
            Settings = settings,
            InjectionMode = (AIContextResourceInjectionMode)entity.InjectionMode
        };
    }

    /// <inheritdoc />
    public AIContextEntity BuildEntity(AIContext context)
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

    /// <inheritdoc />
    public AIContextResourceEntity BuildResourceEntity(AIContextResource resource, Guid contextId)
    {
        return new AIContextResourceEntity
        {
            Id = resource.Id,
            ContextId = contextId,
            ResourceTypeId = resource.ResourceTypeId,
            Name = resource.Name,
            Description = resource.Description,
            SortOrder = resource.SortOrder,
            Settings = SerializeSettings(resource),
            InjectionMode = (int)resource.InjectionMode
        };
    }

    /// <inheritdoc />
    public void UpdateEntity(AIContextEntity entity, AIContext context)
    {
        entity.Alias = context.Alias;
        entity.Name = context.Name;
        entity.DateModified = context.DateModified;
        entity.ModifiedByUserId = context.ModifiedByUserId;
        entity.Version = context.Version;
        // Resources are handled separately in the repository
        // DateCreated and CreatedByUserId are intentionally not updated
    }

    /// <inheritdoc />
    public void UpdateResourceEntity(AIContextResourceEntity entity, AIContextResource resource)
    {
        entity.ResourceTypeId = resource.ResourceTypeId;
        entity.Name = resource.Name;
        entity.Description = resource.Description;
        entity.SortOrder = resource.SortOrder;
        entity.Settings = SerializeSettings(resource);
        entity.InjectionMode = (int)resource.InjectionMode;
    }

    /// <summary>
    /// Serializes a resource's settings, encrypting sensitive fields based on the resource type schema.
    /// </summary>
    private string SerializeSettings(AIContextResource resource)
    {
        if (resource.Settings is null)
        {
            return string.Empty;
        }

        var schema = GetSettingsSchema(resource.ResourceTypeId);
        return _serializer.Serialize(resource.Settings, schema) ?? string.Empty;
    }

    private AIEditableModelSchema? GetSettingsSchema(string resourceTypeId)
        => _resourceTypes.GetById(resourceTypeId)?.GetSettingsSchema();
}
