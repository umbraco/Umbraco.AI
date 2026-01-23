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

    /// <summary>
    /// Creates a JSON snapshot of a prompt for version history storage.
    /// </summary>
    /// <param name="aiPrompt">The prompt to snapshot.</param>
    /// <returns>JSON string representing the prompt state.</returns>
    public static string CreateSnapshot(Core.Prompts.AiPrompt aiPrompt)
    {
        var snapshot = new
        {
            aiPrompt.Id,
            aiPrompt.Alias,
            aiPrompt.Name,
            aiPrompt.Description,
            aiPrompt.Instructions,
            aiPrompt.ProfileId,
            ContextIds = SerializeContextIds(aiPrompt.ContextIds),
            Tags = SerializeTags(aiPrompt.Tags),
            aiPrompt.IsActive,
            aiPrompt.IncludeEntityContext,
            Scope = SerializeScope(aiPrompt.Scope),
            aiPrompt.DateCreated,
            aiPrompt.DateModified,
            aiPrompt.CreatedByUserId,
            aiPrompt.ModifiedByUserId,
            aiPrompt.Version
        };

        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    /// <summary>
    /// Creates a domain model from a JSON snapshot.
    /// </summary>
    /// <param name="json">The JSON snapshot.</param>
    /// <returns>The prompt domain model, or null if parsing fails.</returns>
    public static Core.Prompts.AiPrompt? BuildDomainFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tags = root.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.String
                ? DeserializeTags(tagsEl.GetString())
                : Array.Empty<string>();

            var contextIds = root.TryGetProperty("contextIds", out var ctxEl) && ctxEl.ValueKind == JsonValueKind.String
                ? DeserializeContextIds(ctxEl.GetString())
                : Array.Empty<Guid>();

            var scope = root.TryGetProperty("scope", out var scopeEl) && scopeEl.ValueKind == JsonValueKind.String
                ? DeserializeScope(scopeEl.GetString())
                : null;

            return new Core.Prompts.AiPrompt
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                Description = root.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                    ? descEl.GetString() : null,
                Instructions = root.TryGetProperty("instructions", out var instrEl) && instrEl.ValueKind == JsonValueKind.String
                    ? instrEl.GetString() : null,
                ProfileId = root.TryGetProperty("profileId", out var profEl) && profEl.ValueKind != JsonValueKind.Null
                    ? profEl.GetGuid() : null,
                ContextIds = contextIds,
                Tags = tags,
                IsActive = root.GetProperty("isActive").GetBoolean(),
                IncludeEntityContext = root.GetProperty("includeEntityContext").GetBoolean(),
                Scope = scope,
                DateCreated = root.GetProperty("dateCreated").GetDateTime(),
                DateModified = root.GetProperty("dateModified").GetDateTime(),
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null
                    ? cbu.GetInt32() : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null
                    ? mbu.GetInt32() : null,
                Version = root.GetProperty("version").GetInt32()
            };
        }
        catch
        {
            return null;
        }
    }
}
