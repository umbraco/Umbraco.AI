using System.Text.Json;
using Umbraco.Ai.Prompt.Core.Prompts;

namespace Umbraco.Ai.Prompt.Persistence.Prompts;

/// <summary>
/// Factory for converting between <see cref="Prompt"/> domain model and <see cref="PromptEntity"/>.
/// </summary>
internal static class PromptEntityFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Builds a domain model from an entity.
    /// </summary>
    public static Core.Prompts.Prompt BuildDomain(PromptEntity entity)
    {
        var tags = DeserializeTags(entity.TagsJson);

        return new Core.Prompts.Prompt
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            Content = entity.Content,
            ProfileId = entity.ProfileId,
            Tags = tags,
            IsActive = entity.IsActive,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified
        };
    }

    /// <summary>
    /// Builds an entity from a domain model.
    /// </summary>
    public static PromptEntity BuildEntity(Core.Prompts.Prompt prompt)
    {
        return new PromptEntity
        {
            Id = prompt.Id,
            Alias = prompt.Alias,
            Name = prompt.Name,
            Description = prompt.Description,
            Content = prompt.Content,
            ProfileId = prompt.ProfileId,
            TagsJson = SerializeTags(prompt.Tags),
            IsActive = prompt.IsActive,
            DateCreated = prompt.DateCreated,
            DateModified = prompt.DateModified
        };
    }

    /// <summary>
    /// Updates an existing entity from a domain model.
    /// </summary>
    public static void UpdateEntity(PromptEntity entity, Core.Prompts.Prompt prompt)
    {
        entity.Name = prompt.Name;
        entity.Description = prompt.Description;
        entity.Content = prompt.Content;
        entity.ProfileId = prompt.ProfileId;
        entity.TagsJson = SerializeTags(prompt.Tags);
        entity.IsActive = prompt.IsActive;
        entity.DateModified = prompt.DateModified;
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
}
