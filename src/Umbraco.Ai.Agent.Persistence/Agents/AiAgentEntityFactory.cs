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
        var tags = DeserializeTags(entity.Tags);
        var scope = DeserializeScope(entity.Scope);

        return new Core.Agents.AiAgent
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            Content = entity.Content,
            ProfileId = entity.ProfileId,
            Tags = tags,
            IsActive = entity.IsActive,
            Scope = scope,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified
        };
    }

    /// <summary>
    /// Builds an entity from a domain model.
    /// </summary>
    public static AiAgentEntity BuildEntity(Core.Agents.AiAgent AiAgent)
    {
        return new AiAgentEntity
        {
            Id = AiAgent.Id,
            Alias = AiAgent.Alias,
            Name = AiAgent.Name,
            Description = AiAgent.Description,
            Content = AiAgent.Content,
            ProfileId = AiAgent.ProfileId,
            Tags = SerializeTags(AiAgent.Tags),
            IsActive = AiAgent.IsActive,
            Scope = SerializeScope(AiAgent.Scope),
            DateCreated = AiAgent.DateCreated,
            DateModified = AiAgent.DateModified
        };
    }

    /// <summary>
    /// Updates an existing entity from a domain model.
    /// </summary>
    public static void UpdateEntity(AiAgentEntity entity, Core.Agents.AiAgent AiAgent)
    {
        entity.Alias = AiAgent.Alias;
        entity.Name = AiAgent.Name;
        entity.Description = AiAgent.Description;
        entity.Content = AiAgent.Content;
        entity.ProfileId = AiAgent.ProfileId;
        entity.Tags = SerializeTags(AiAgent.Tags);
        entity.IsActive = AiAgent.IsActive;
        entity.Scope = SerializeScope(AiAgent.Scope);
        entity.DateModified = AiAgent.DateModified;
    }

    private static string? SerializeTags(IReadOnlyList<string> tags)
    {
        return tags.Count == 0 ? null : string.Join(',', tags);
    }

    private static IReadOnlyList<string> DeserializeTags(string? tags)
    {
        return string.IsNullOrWhiteSpace(tags) ? [] : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? SerializeScope(AiAgentScope? scope)
    {
        if (scope is null)
        {
            return null;
        }

        // Don't store empty scope as JSON - treat as null
        if (scope.AllowRules.Count == 0 && scope.DenyRules.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(scope, JsonOptions);
    }

    private static AiAgentScope? DeserializeScope(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiAgentScope>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
