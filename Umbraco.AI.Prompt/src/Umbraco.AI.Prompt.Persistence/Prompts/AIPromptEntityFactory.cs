using System.Text.Json;
using Umbraco.AI.Prompt.Core.Prompts;

namespace Umbraco.AI.Prompt.Persistence.Prompts;

/// <summary>
/// Factory for converting between <see cref="AIPrompt"/> domain model and <see cref="AIPromptEntity"/>.
/// </summary>
internal static class AIPromptEntityFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Builds a domain model from an entity.
    /// </summary>
    public static Core.Prompts.AIPrompt BuildDomain(AIPromptEntity entity)
    {
        var tags = DeserializeTags(entity.Tags);
        var scope = DeserializeScope(entity.Scope);
        var contextIds = DeserializeContextIds(entity.ContextIds);

        return new Core.Prompts.AIPrompt
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            Instructions = entity.Instructions,
            ProfileId = entity.ProfileId,
            ContextIds = contextIds,
            Tags = tags,
            IsActive = entity.IsActive,
            IncludeEntityContext = entity.IncludeEntityContext,
            Scope = scope,
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
    public static AIPromptEntity BuildEntity(Core.Prompts.AIPrompt aiPrompt)
    {
        return new AIPromptEntity
        {
            Id = aiPrompt.Id,
            Alias = aiPrompt.Alias,
            Name = aiPrompt.Name,
            Description = aiPrompt.Description,
            Instructions = aiPrompt.Instructions,
            ProfileId = aiPrompt.ProfileId,
            ContextIds = SerializeContextIds(aiPrompt.ContextIds),
            Tags = SerializeTags(aiPrompt.Tags),
            IsActive = aiPrompt.IsActive,
            IncludeEntityContext = aiPrompt.IncludeEntityContext,
            Scope = SerializeScope(aiPrompt.Scope),
            DateCreated = aiPrompt.DateCreated,
            DateModified = aiPrompt.DateModified,
            CreatedByUserId = aiPrompt.CreatedByUserId,
            ModifiedByUserId = aiPrompt.ModifiedByUserId,
            Version = aiPrompt.Version
        };
    }

    /// <summary>
    /// Updates an existing entity from a domain model.
    /// </summary>
    public static void UpdateEntity(AIPromptEntity entity, Core.Prompts.AIPrompt aiPrompt)
    {
        entity.Alias = aiPrompt.Alias;
        entity.Name = aiPrompt.Name;
        entity.Description = aiPrompt.Description;
        entity.Instructions = aiPrompt.Instructions;
        entity.ProfileId = aiPrompt.ProfileId;
        entity.ContextIds = SerializeContextIds(aiPrompt.ContextIds);
        entity.Tags = SerializeTags(aiPrompt.Tags);
        entity.IsActive = aiPrompt.IsActive;
        entity.IncludeEntityContext = aiPrompt.IncludeEntityContext;
        entity.Scope = SerializeScope(aiPrompt.Scope);
        entity.DateModified = aiPrompt.DateModified;
        entity.ModifiedByUserId = aiPrompt.ModifiedByUserId;
        entity.Version = aiPrompt.Version;
        // DateCreated and CreatedByUserId are intentionally not updated
    }

    private static string? SerializeTags(IReadOnlyList<string> tags)
    {
        return tags.Count == 0 ? null : string.Join(',', tags);
    }

    private static IReadOnlyList<string> DeserializeTags(string? tags)
    {
        return string.IsNullOrWhiteSpace(tags) ? [] : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? SerializeScope(AIPromptScope? scope)
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

    private static AIPromptScope? DeserializeScope(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AIPromptScope>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeContextIds(IReadOnlyList<Guid> contextIds)
    {
        return contextIds.Count == 0 ? null : JsonSerializer.Serialize(contextIds, JsonOptions);
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
