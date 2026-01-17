using System.Text.Json;
using Umbraco.Ai.Prompt.Core.Prompts;

namespace Umbraco.Ai.Prompt.Persistence.Prompts;

/// <summary>
/// Factory for converting between <see cref="AiPrompt"/> domain model and <see cref="AiPromptEntity"/>.
/// </summary>
internal static class AiPromptEntityFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Builds a domain model from an entity.
    /// </summary>
    public static Core.Prompts.AiPrompt BuildDomain(AiPromptEntity entity)
    {
        var tags = DeserializeTags(entity.Tags);
        var scope = DeserializeScope(entity.Scope);
        var contextIds = DeserializeContextIds(entity.ContextIds);

        return new Core.Prompts.AiPrompt
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
            Scope = scope,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified
        };
    }

    /// <summary>
    /// Builds an entity from a domain model.
    /// </summary>
    public static AiPromptEntity BuildEntity(Core.Prompts.AiPrompt aiPrompt)
    {
        return new AiPromptEntity
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
            Scope = SerializeScope(aiPrompt.Scope),
            DateCreated = aiPrompt.DateCreated,
            DateModified = aiPrompt.DateModified
        };
    }

    /// <summary>
    /// Updates an existing entity from a domain model.
    /// </summary>
    public static void UpdateEntity(AiPromptEntity entity, Core.Prompts.AiPrompt aiPrompt)
    {
        entity.Alias = aiPrompt.Alias;
        entity.Name = aiPrompt.Name;
        entity.Description = aiPrompt.Description;
        entity.Instructions = aiPrompt.Instructions;
        entity.ProfileId = aiPrompt.ProfileId;
        entity.ContextIds = SerializeContextIds(aiPrompt.ContextIds);
        entity.Tags = SerializeTags(aiPrompt.Tags);
        entity.IsActive = aiPrompt.IsActive;
        entity.Scope = SerializeScope(aiPrompt.Scope);
        entity.DateModified = aiPrompt.DateModified;
    }

    private static string? SerializeTags(IReadOnlyList<string> tags)
    {
        return tags.Count == 0 ? null : string.Join(',', tags);
    }

    private static IReadOnlyList<string> DeserializeTags(string? tags)
    {
        return string.IsNullOrWhiteSpace(tags) ? [] : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? SerializeScope(AiPromptScope? scope)
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

    private static AiPromptScope? DeserializeScope(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiPromptScope>(json, JsonOptions);
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
