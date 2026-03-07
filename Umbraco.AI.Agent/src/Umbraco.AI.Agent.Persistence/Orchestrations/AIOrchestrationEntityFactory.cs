using System.Text.Json;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Orchestrations;

namespace Umbraco.AI.Agent.Persistence.Orchestrations;

/// <summary>
/// Factory for converting between <see cref="AIOrchestration"/> domain model and <see cref="AIOrchestrationEntity"/>.
/// </summary>
internal static class AIOrchestrationEntityFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Builds a domain model from an entity.
    /// </summary>
    public static AIOrchestration BuildDomain(AIOrchestrationEntity entity)
    {
        return new AIOrchestration
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            ProfileId = entity.ProfileId,
            SurfaceIds = DeserializeSurfaceIds(entity.SurfaceIds),
            Scope = DeserializeScope(entity.Scope),
            Graph = DeserializeGraph(entity.Graph),
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
    public static AIOrchestrationEntity BuildEntity(AIOrchestration orchestration)
    {
        return new AIOrchestrationEntity
        {
            Id = orchestration.Id,
            Alias = orchestration.Alias,
            Name = orchestration.Name,
            Description = orchestration.Description,
            ProfileId = orchestration.ProfileId,
            SurfaceIds = SerializeSurfaceIds(orchestration.SurfaceIds),
            Scope = SerializeScope(orchestration.Scope),
            Graph = SerializeGraph(orchestration.Graph),
            IsActive = orchestration.IsActive,
            DateCreated = orchestration.DateCreated,
            DateModified = orchestration.DateModified,
            CreatedByUserId = orchestration.CreatedByUserId,
            ModifiedByUserId = orchestration.ModifiedByUserId,
            Version = orchestration.Version
        };
    }

    /// <summary>
    /// Updates an existing entity from a domain model.
    /// </summary>
    public static void UpdateEntity(AIOrchestrationEntity entity, AIOrchestration orchestration)
    {
        entity.Alias = orchestration.Alias;
        entity.Name = orchestration.Name;
        entity.Description = orchestration.Description;
        entity.ProfileId = orchestration.ProfileId;
        entity.SurfaceIds = SerializeSurfaceIds(orchestration.SurfaceIds);
        entity.Scope = SerializeScope(orchestration.Scope);
        entity.Graph = SerializeGraph(orchestration.Graph);
        entity.IsActive = orchestration.IsActive;
        entity.DateModified = orchestration.DateModified;
        entity.ModifiedByUserId = orchestration.ModifiedByUserId;
        entity.Version = orchestration.Version;
        // DateCreated and CreatedByUserId are intentionally not updated
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

    private static string? SerializeGraph(AIOrchestrationGraph graph)
    {
        if (graph.Nodes.Count == 0 && graph.Edges.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(graph, JsonOptions);
    }

    private static AIOrchestrationGraph DeserializeGraph(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AIOrchestrationGraph();
        }

        try
        {
            return JsonSerializer.Deserialize<AIOrchestrationGraph>(json, JsonOptions) ?? new AIOrchestrationGraph();
        }
        catch
        {
            return new AIOrchestrationGraph();
        }
    }
}
