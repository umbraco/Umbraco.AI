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
            ScopeIds = DeserializeScopeIds(entity.ScopeIds),
            EnabledToolIds = DeserializeEnabledToolIds(entity.EnabledToolIds),
            EnabledToolScopeIds = DeserializeEnabledToolScopeIds(entity.EnabledToolScopeIds),
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
            ScopeIds = SerializeScopeIds(aiAgent.ScopeIds),
            EnabledToolIds = SerializeEnabledToolIds(aiAgent.EnabledToolIds),
            EnabledToolScopeIds = SerializeEnabledToolScopeIds(aiAgent.EnabledToolScopeIds),
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
        entity.ScopeIds = SerializeScopeIds(aiAgent.ScopeIds);
        entity.EnabledToolIds = SerializeEnabledToolIds(aiAgent.EnabledToolIds);
        entity.EnabledToolScopeIds = SerializeEnabledToolScopeIds(aiAgent.EnabledToolScopeIds);
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

    private static string? SerializeScopeIds(IReadOnlyList<string> scopeIds)
    {
        if (scopeIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(scopeIds, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeScopeIds(string? json)
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

    private static string? SerializeEnabledToolIds(IReadOnlyList<string> toolIds)
    {
        if (toolIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(toolIds, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeEnabledToolIds(string? json)
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

    private static string? SerializeEnabledToolScopeIds(IReadOnlyList<string> scopeIds)
    {
        if (scopeIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(scopeIds, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeEnabledToolScopeIds(string? json)
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
}
