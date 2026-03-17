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
        var agentType = (AIAgentType)entity.AgentType;

        return new Core.Agents.AIAgent
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            AgentType = agentType,
            Config = AIAgentConfigSerializer.Deserialize(agentType, entity.Config),
            ProfileId = entity.ProfileId,
            GuardrailIds = DeserializeGuardrailIds(entity.GuardrailIds),
            SurfaceIds = DeserializeSurfaceIds(entity.SurfaceIds),
            Scope = DeserializeScope(entity.Scope),
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
            AgentType = (int)aiAgent.AgentType,
            Config = AIAgentConfigSerializer.Serialize(aiAgent.Config),
            ProfileId = aiAgent.ProfileId,
            GuardrailIds = SerializeGuardrailIds(aiAgent.GuardrailIds),
            SurfaceIds = SerializeSurfaceIds(aiAgent.SurfaceIds),
            Scope = SerializeScope(aiAgent.Scope),
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
        // AgentType is intentionally not updated (immutable after creation)
        entity.Config = AIAgentConfigSerializer.Serialize(aiAgent.Config);
        entity.ProfileId = aiAgent.ProfileId;
        entity.GuardrailIds = SerializeGuardrailIds(aiAgent.GuardrailIds);
        entity.SurfaceIds = SerializeSurfaceIds(aiAgent.SurfaceIds);
        entity.Scope = SerializeScope(aiAgent.Scope);
        entity.IsActive = aiAgent.IsActive;
        entity.DateModified = aiAgent.DateModified;
        entity.ModifiedByUserId = aiAgent.ModifiedByUserId;
        entity.Version = aiAgent.Version;
        // DateCreated and CreatedByUserId are intentionally not updated
    }

    private static string? SerializeGuardrailIds(IReadOnlyList<Guid> guardrailIds)
    {
        if (guardrailIds.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(guardrailIds, JsonOptions);
    }

    private static IReadOnlyList<Guid> DeserializeGuardrailIds(string? json)
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

    private static string? SerializeScope(AIAgentScope? scope)
    {
        if (scope is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(scope, JsonOptions);
    }

    private static AIAgentScope? DeserializeScope(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AIAgentScope>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
