using System.Text.Json;
using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Agent.Conversations.Persistence.Projects;

/// <summary>
/// Maps between <see cref="AIProject"/> domain models and their EF Core entities. Handles the same
/// schema-driven encryption/decryption of resource settings as <c>AIContextFactory</c> (reusing the
/// public resource-type collection + editable-model serializer), plus JSON (de)serialization of the
/// referenced-context id list.
/// </summary>
internal sealed class AIProjectFactory
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AIContextResourceTypeCollection _resourceTypes;

    public AIProjectFactory(
        IAIEditableModelSerializer serializer,
        AIContextResourceTypeCollection resourceTypes)
    {
        _serializer = serializer;
        _resourceTypes = resourceTypes;
    }

    public AIProject BuildDomain(AIProjectEntity entity, IEnumerable<AIProjectResourceEntity> resources) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Instructions = entity.Instructions,
        UserKey = entity.UserKey,
        ContextIds = DeserializeContextIds(entity.ContextIds),
        Resources = resources
            .OrderBy(r => r.SortOrder)
            .Select(BuildResourceDomain)
            .ToList(),
        DateCreated = entity.DateCreated,
        DateModified = entity.DateModified,
        Version = entity.Version,
    };

    public AIProjectEntity BuildEntity(AIProject project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        Instructions = project.Instructions,
        UserKey = project.UserKey,
        ContextIds = SerializeContextIds(project.ContextIds),
        DateCreated = project.DateCreated,
        DateModified = project.DateModified,
        Version = project.Version,
    };

    public void UpdateEntity(AIProjectEntity entity, AIProject project)
    {
        entity.Name = project.Name;
        entity.Description = project.Description;
        entity.Instructions = project.Instructions;
        entity.ContextIds = SerializeContextIds(project.ContextIds);
        entity.DateModified = project.DateModified;
        entity.Version = project.Version;
        // UserKey and DateCreated are intentionally not updated.
    }

    public AIProjectResourceEntity BuildResourceEntity(AIAttachedResource resource, Guid projectId) => new()
    {
        Id = resource.Id,
        ProjectId = projectId,
        ResourceTypeId = resource.ResourceTypeId,
        Name = resource.Name,
        Description = resource.Description,
        SortOrder = resource.SortOrder,
        Settings = SerializeSettings(resource),
        InjectionMode = (int)resource.InjectionMode,
    };

    public void UpdateResourceEntity(AIProjectResourceEntity entity, AIAttachedResource resource)
    {
        entity.ResourceTypeId = resource.ResourceTypeId;
        entity.Name = resource.Name;
        entity.Description = resource.Description;
        entity.SortOrder = resource.SortOrder;
        entity.Settings = SerializeSettings(resource);
        entity.InjectionMode = (int)resource.InjectionMode;
    }

    private AIAttachedResource BuildResourceDomain(AIProjectResourceEntity entity)
    {
        // Settings are stored as JSON with sensitive fields encrypted. The serializer decrypts any
        // encrypted values; typed deserialization happens later at the resource-type layer.
        object? settings = _serializer.Deserialize(entity.Settings);

        return new AIAttachedResource
        {
            Id = entity.Id,
            ResourceTypeId = entity.ResourceTypeId,
            Name = entity.Name,
            Description = entity.Description,
            SortOrder = entity.SortOrder,
            Settings = settings,
            InjectionMode = (AIContextResourceInjectionMode)entity.InjectionMode,
        };
    }

    private string SerializeSettings(AIAttachedResource resource)
    {
        if (resource.Settings is null)
        {
            return string.Empty;
        }

        AIEditableModelSchema? schema = _resourceTypes.GetById(resource.ResourceTypeId)?.GetSettingsSchema();
        return _serializer.Serialize(resource.Settings, schema) ?? string.Empty;
    }

    private static string? SerializeContextIds(IList<Guid> contextIds)
        => contextIds.Count == 0 ? null : JsonSerializer.Serialize(contextIds);

    private static IList<Guid> DeserializeContextIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
