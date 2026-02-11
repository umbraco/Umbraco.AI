using System.Text.Json;
using Umbraco.AI.Agent.Core.Agents;

namespace Umbraco.AI.Agent.Persistence.Agents;

/// <summary>
/// Factory for converting between <see cref="AIAgent"/> domain model and <see cref="AIAgentEntity"/>.
/// </summary>
internal static class AIAgentEntityFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Builds a domain model from an entity.
    /// </summary>
    public static Core.Agents.AIAgent BuildDomain(AIAgentEntity entity)
    {
        return new Core.Agents.AIAgent
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            ProfileId = entity.ProfileId,
            ContextIds = DeserializeContextIds(entity.ContextIds),
            SurfaceIds = DeserializeSurfaceIds(entity.SurfaceIds),
            ContextScope = DeserializeContextScope(entity.ContextScope),
            AllowedToolIds = DeserializeAllowedToolIds(entity.AllowedToolIds),
            AllowedToolScopeIds = DeserializeAllowedToolScopeIds(entity.AllowedToolScopeIds),
            UserGroupPermissions = DeserializeUserGroupPermissions(entity.UserGroupPermissions),
            Instructions = entity.Instructions,
            IsActive = entity.IsActive,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId,
            Version = entity.Version
        };
    }

    /// <summary>
    /// Builds an entity from a domain model.
    /// </summary>
    public static AIAgentEntity BuildEntity(Core.Agents.AIAgent aiAgent)
    {
        return new AIAgentEntity
        {
            Id = aiAgent.Id,
            Alias = aiAgent.Alias,
            Name = aiAgent.Name,
            Description = aiAgent.Description,
            ProfileId = aiAgent.ProfileId,
            ContextIds = SerializeContextIds(aiAgent.ContextIds),
            SurfaceIds = SerializeSurfaceIds(aiAgent.SurfaceIds),
            ContextScope = SerializeContextScope(aiAgent.ContextScope),
            AllowedToolIds = SerializeAllowedToolIds(aiAgent.AllowedToolIds),
            AllowedToolScopeIds = SerializeAllowedToolScopeIds(aiAgent.AllowedToolScopeIds),
            UserGroupPermissions = SerializeUserGroupPermissions(aiAgent.UserGroupPermissions),
            Instructions = aiAgent.Instructions,
            IsActive = aiAgent.IsActive,
            DateCreated = aiAgent.DateCreated,
            DateModified = aiAgent.DateModified,
            CreatedByUserId = aiAgent.CreatedByUserId,
            ModifiedByUserId = aiAgent.ModifiedByUserId,
            Version = aiAgent.Version
        };
    }

    /// <summary>
    /// Updates an existing entity from a domain model.
    /// </summary>
    public static void UpdateEntity(AIAgentEntity entity, Core.Agents.AIAgent aiAgent)
    {
        entity.Alias = aiAgent.Alias;
        entity.Name = aiAgent.Name;
        entity.Description = aiAgent.Description;
        entity.ProfileId = aiAgent.ProfileId;
        entity.ContextIds = SerializeContextIds(aiAgent.ContextIds);
        entity.SurfaceIds = SerializeSurfaceIds(aiAgent.SurfaceIds);
        entity.ContextScope = SerializeContextScope(aiAgent.ContextScope);
        entity.AllowedToolIds = SerializeAllowedToolIds(aiAgent.AllowedToolIds);
        entity.AllowedToolScopeIds = SerializeAllowedToolScopeIds(aiAgent.AllowedToolScopeIds);
        entity.UserGroupPermissions = SerializeUserGroupPermissions(aiAgent.UserGroupPermissions);
        entity.Instructions = aiAgent.Instructions;
        entity.IsActive = aiAgent.IsActive;
        entity.DateModified = aiAgent.DateModified;
        entity.ModifiedByUserId = aiAgent.ModifiedByUserId;
        entity.Version = aiAgent.Version;
        // DateCreated and CreatedByUserId are intentionally not updated
    }

    private static string? SerializeContextIds(IReadOnlyList<Guid> contextIds)
    {
        if (contextIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(contextIds, JsonOptions);
    }

    private static IReadOnlyList<Guid> DeserializeContextIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? SerializeSurfaceIds(IReadOnlyList<string> surfaceIds)
    {
        if (surfaceIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(surfaceIds, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeSurfaceIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? SerializeAllowedToolIds(IReadOnlyList<string> toolIds)
    {
        if (toolIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(toolIds, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeAllowedToolIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? SerializeAllowedToolScopeIds(IReadOnlyList<string> scopeIds)
    {
        if (scopeIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(scopeIds, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeAllowedToolScopeIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? SerializeUserGroupPermissions(IReadOnlyDictionary<Guid, AIAgentUserGroupPermissions> permissions)
    {
        if (permissions.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(permissions, JsonOptions);
    }

    private static IReadOnlyDictionary<Guid, AIAgentUserGroupPermissions> DeserializeUserGroupPermissions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<Guid, AIAgentUserGroupPermissions>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<Guid, AIAgentUserGroupPermissions>>(json, JsonOptions)
                ?? new Dictionary<Guid, AIAgentUserGroupPermissions>();
        }
        catch
        {
            return new Dictionary<Guid, AIAgentUserGroupPermissions>();
        }
    }

    private static string? SerializeContextScope(AIAgentContextScope? contextScope)
    {
        if (contextScope is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(contextScope, JsonOptions);
    }

    private static AIAgentContextScope? DeserializeContextScope(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AIAgentContextScope>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
