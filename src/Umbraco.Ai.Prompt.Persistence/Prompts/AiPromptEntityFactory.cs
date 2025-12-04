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
        var tags = DeserializeTags(entity.TagsJson);
        var scope = DeserializeScope(entity.ScopeJson);

        return new Core.Prompts.AiPrompt
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
    public static AiPromptEntity BuildEntity(Core.Prompts.AiPrompt aiPrompt)
    {
        return new AiPromptEntity
        {
            Id = aiPrompt.Id,
            Alias = aiPrompt.Alias,
            Name = aiPrompt.Name,
            Description = aiPrompt.Description,
            Content = aiPrompt.Content,
            ProfileId = aiPrompt.ProfileId,
            TagsJson = SerializeTags(aiPrompt.Tags),
            IsActive = aiPrompt.IsActive,
            ScopeJson = SerializeScope(aiPrompt.Scope),
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
        entity.Content = aiPrompt.Content;
        entity.ProfileId = aiPrompt.ProfileId;
        entity.TagsJson = SerializeTags(aiPrompt.Tags);
        entity.IsActive = aiPrompt.IsActive;
        entity.ScopeJson = SerializeScope(aiPrompt.Scope);
        entity.DateModified = aiPrompt.DateModified;
    }

    private static string? SerializeTags(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(tags, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeTags(string? json)
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

    private static string? SerializeScope(AiPromptScope? scope)
    {
        if (scope is null)
        {
            return null;
        }

        // Don't store empty scopes as JSON - treat as null
        if (scope.IncludeRules.Count == 0 && scope.ExcludeRules.Count == 0)
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
}
