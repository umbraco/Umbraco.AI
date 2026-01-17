using System.Text.Json;
using Umbraco.Ai.Agent.Core.Agents;

namespace Umbraco.Ai.Agent.Persistence.Agents;

/// <summary>
/// Factory for converting between <see cref="AiAgent"/> domain model and <see cref="AiAgentEntity"/>.
/// </summary>
internal static class AiAgentEntityFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Builds a domain model from an entity.
    /// </summary>
    public static Core.Agents.AiAgent BuildDomain(AiAgentEntity entity)
    {
        return new Core.Agents.AiAgent
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            ProfileId = entity.ProfileId,
            ContextIds = DeserializeContextIds(entity.ContextIds),
            Instructions = entity.Instructions,
            IsActive = entity.IsActive,
        };
    }

    /// <summary>
    /// Builds an entity from a domain model.
    /// </summary>
    public static AiAgentEntity BuildEntity(Core.Agents.AiAgent aiAgent)
    {
        return new AiAgentEntity
        {
            Id = aiAgent.Id,
            Alias = aiAgent.Alias,
            Name = aiAgent.Name,
            Description = aiAgent.Description,
            ProfileId = aiAgent.ProfileId,
            ContextIds = SerializeContextIds(aiAgent.ContextIds),
            Instructions = aiAgent.Instructions,
            IsActive = aiAgent.IsActive
        };
    }

    /// <summary>
    /// Updates an existing entity from a domain model.
    /// </summary>
    public static void UpdateEntity(AiAgentEntity entity, Core.Agents.AiAgent aiAgent)
    {
        entity.Alias = aiAgent.Alias;
        entity.Name = aiAgent.Name;
        entity.Description = aiAgent.Description;
        entity.ProfileId = aiAgent.ProfileId;
        entity.ContextIds = SerializeContextIds(aiAgent.ContextIds);
        entity.Instructions = aiAgent.Instructions;
        entity.IsActive = aiAgent.IsActive;
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
}
