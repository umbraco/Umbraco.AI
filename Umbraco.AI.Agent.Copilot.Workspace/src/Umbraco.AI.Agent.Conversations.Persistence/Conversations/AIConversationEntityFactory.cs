using System.Text.Json;
using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Agent.Conversations.Persistence.Conversations;

/// <summary>
/// Converts between <see cref="AIConversation"/> and <see cref="AIConversationEntity"/>. Conversation
/// context ids (JSON column) and directly-attached resources (separate table) are handled the same way
/// as <c>AIProjectFactory</c> — reusing the public resource-type collection and editable-model
/// serializer for schema-driven, sensitive-field-encrypting settings (de)serialization.
/// </summary>
internal sealed class AIConversationEntityFactory
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AIContextResourceTypeCollection _resourceTypes;

    public AIConversationEntityFactory(
        IAIEditableModelSerializer serializer,
        AIContextResourceTypeCollection resourceTypes)
    {
        _serializer = serializer;
        _resourceTypes = resourceTypes;
    }

    public AIConversation BuildDomain(
        AIConversationEntity entity,
        IEnumerable<AIConversationResourceEntity> resources) => new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        Title = entity.Title,
        UserKey = entity.UserKey,
        AgentIdOrAlias = entity.AgentIdOrAlias,
        ProfileId = entity.ProfileId,
        ContextIds = DeserializeContextIds(entity.ContextIds),
        Resources = resources
            .OrderBy(r => r.SortOrder)
            .Select(BuildResourceDomain)
            .ToList(),
        IsPinned = entity.IsPinned,
        IsArchived = entity.IsArchived,
        DateCreated = entity.DateCreated,
        DateModified = entity.DateModified,
        LastMessageAt = entity.LastMessageAt,
        Version = entity.Version,
    };

    public AIConversationEntity BuildEntity(AIConversation domain) => new()
    {
        Id = domain.Id,
        ProjectId = domain.ProjectId,
        Title = domain.Title,
        UserKey = domain.UserKey,
        AgentIdOrAlias = domain.AgentIdOrAlias,
        ProfileId = domain.ProfileId,
        ContextIds = SerializeContextIds(domain.ContextIds),
        IsPinned = domain.IsPinned,
        IsArchived = domain.IsArchived,
        DateCreated = domain.DateCreated,
        DateModified = domain.DateModified,
        LastMessageAt = domain.LastMessageAt,
        Version = domain.Version,
    };

    public AIConversationResourceEntity BuildResourceEntity(AIAttachedResource resource, Guid conversationId) => new()
    {
        Id = resource.Id,
        ConversationId = conversationId,
        ResourceTypeId = resource.ResourceTypeId,
        Name = resource.Name,
        Description = resource.Description,
        SortOrder = resource.SortOrder,
        Settings = SerializeSettings(resource),
        InjectionMode = (int)resource.InjectionMode,
    };

    public void UpdateResourceEntity(AIConversationResourceEntity entity, AIAttachedResource resource)
    {
        entity.ResourceTypeId = resource.ResourceTypeId;
        entity.Name = resource.Name;
        entity.Description = resource.Description;
        entity.SortOrder = resource.SortOrder;
        entity.Settings = SerializeSettings(resource);
        entity.InjectionMode = (int)resource.InjectionMode;
    }

    private AIAttachedResource BuildResourceDomain(AIConversationResourceEntity entity)
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
