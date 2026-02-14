using System.Text.Json;
using Umbraco.AI.Core.Versioning;

using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Versionable entity adapter for AI prompts.
/// </summary>
internal sealed class AIPromptVersionableEntityAdapter : AIVersionableEntityAdapterBase<AIPrompt>
{
    private readonly IAIPromptService _promptService;
    private readonly IAIEntityVersionService _versionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="promptService">The prompt service for save operations.</param>
    /// <param name="versionService">The entity version service for retrieving snapshots.</param>
    public AIPromptVersionableEntityAdapter(IAIPromptService promptService, IAIEntityVersionService versionService)
    {
        _promptService = promptService;
        _versionService = versionService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AIPrompt entity)
    {
        var snapshot = new
        {
            entity.Id,
            entity.Alias,
            entity.Name,
            entity.Description,
            entity.Instructions,
            entity.ProfileId,
            ContextIds = entity.ContextIds.Count > 0 ? string.Join(',', entity.ContextIds) : null,
            Tags = entity.Tags.Count > 0 ? string.Join(',', entity.Tags) : null,
            entity.IsActive,
            entity.IncludeEntityContext,
            Scope = entity.Scope is not null ? SerializeScope(entity.Scope) : null,
            entity.Version,
            entity.DateCreated,
            entity.DateModified,
            entity.CreatedByUserId,
            entity.ModifiedByUserId
        };

        return JsonSerializer.Serialize(snapshot, CoreConstants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    protected override AIPrompt? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            IReadOnlyList<Guid> contextIds = Array.Empty<Guid>();
            if (root.TryGetProperty("contextIds", out var contextIdsElement) &&
                contextIdsElement.ValueKind == JsonValueKind.String)
            {
                var contextIdsString = contextIdsElement.GetString();
                if (!string.IsNullOrEmpty(contextIdsString))
                {
                    contextIds = contextIdsString
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(Guid.Parse)
                        .ToList();
                }
            }

            IReadOnlyList<string> tags = Array.Empty<string>();
            if (root.TryGetProperty("tags", out var tagsElement) &&
                tagsElement.ValueKind == JsonValueKind.String)
            {
                var tagsString = tagsElement.GetString();
                if (!string.IsNullOrEmpty(tagsString))
                {
                    tags = tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
            }

            AIPromptScope? scope = null;
            if (root.TryGetProperty("scope", out var scopeElement) &&
                scopeElement.ValueKind == JsonValueKind.String)
            {
                var scopeJson = scopeElement.GetString();
                if (!string.IsNullOrEmpty(scopeJson))
                {
                    scope = DeserializeScope(scopeJson);
                }
            }

            return new AIPrompt
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                Description = root.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                    ? descEl.GetString() : null,
                Instructions = root.GetProperty("instructions").GetString()!,
                ProfileId = root.TryGetProperty("profileId", out var profIdEl) && profIdEl.ValueKind != JsonValueKind.Null
                    ? profIdEl.GetGuid() : null,
                ContextIds = contextIds,
                Tags = tags,
                IsActive = root.GetProperty("isActive").GetBoolean(),
                IncludeEntityContext = root.TryGetProperty("includeEntityContext", out var iecEl)
                    ? iecEl.GetBoolean() : true,
                Scope = scope,
                Version = root.GetProperty("version").GetInt32(),
                DateCreated = root.GetProperty("dateCreated").GetDateTime(),
                DateModified = root.GetProperty("dateModified").GetDateTime(),
                // Try Guid first (new format), ignore old int values (no conversion path)
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null && cbu.TryGetGuid(out var cbuGuid)
                    ? cbuGuid : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null && mbu.TryGetGuid(out var mbuGuid)
                    ? mbuGuid : null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<AIValueChange> CompareVersions(AIPrompt from, AIPrompt to)
    {
        var changes = new List<AIValueChange>();

        if (from.Alias != to.Alias)
        {
            changes.Add(new AIValueChange("Alias", from.Alias, to.Alias));
        }

        if (from.Name != to.Name)
        {
            changes.Add(new AIValueChange("Name", from.Name, to.Name));
        }

        if (from.Description != to.Description)
        {
            changes.Add(new AIValueChange("Description", from.Description ?? "(empty)", to.Description ?? "(empty)"));
        }

        if (from.Instructions != to.Instructions)
        {
            changes.Add(new AIValueChange("Instructions", from.Instructions, to.Instructions));
        }

        if (from.ProfileId != to.ProfileId)
        {
            changes.Add(new AIValueChange("ProfileId", from.ProfileId?.ToString() ?? "(none)", to.ProfileId?.ToString() ?? "(none)"));
        }

        // Compare context IDs
        var fromContextIds = string.Join(",", from.ContextIds);
        var toContextIds = string.Join(",", to.ContextIds);
        if (fromContextIds != toContextIds)
        {
            changes.Add(new AIValueChange("ContextIds", fromContextIds.Length > 0 ? fromContextIds : "(none)", toContextIds.Length > 0 ? toContextIds : "(none)"));
        }

        // Compare tags
        var fromTags = string.Join(",", from.Tags);
        var toTags = string.Join(",", to.Tags);
        if (fromTags != toTags)
        {
            changes.Add(new AIValueChange("Tags", fromTags.Length > 0 ? fromTags : "(none)", toTags.Length > 0 ? toTags : "(none)"));
        }

        if (from.IsActive != to.IsActive)
        {
            changes.Add(new AIValueChange("IsActive", from.IsActive.ToString(), to.IsActive.ToString()));
        }

        if (from.IncludeEntityContext != to.IncludeEntityContext)
        {
            changes.Add(new AIValueChange("IncludeEntityContext", from.IncludeEntityContext.ToString(), to.IncludeEntityContext.ToString()));
        }

        // Compare scope
        var fromScopeJson = from.Scope is not null ? SerializeScope(from.Scope) : null;
        var toScopeJson = to.Scope is not null ? SerializeScope(to.Scope) : null;
        if (fromScopeJson != toScopeJson)
        {
            changes.Add(new AIValueChange("Scope", fromScopeJson, toScopeJson));
        }

        return changes;
    }

    /// <inheritdoc />
    public override async Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
    {
        var snapshot = await _versionService.GetVersionSnapshotAsync<AIPrompt>(entityId, version, cancellationToken)
            ?? throw new InvalidOperationException($"Prompt version {version} not found for prompt {entityId}");

        // Save the snapshot as the current version (this will create a new version)
        await _promptService.SavePromptAsync(snapshot, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<AIPrompt?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _promptService.GetPromptAsync(entityId, cancellationToken);

    private static string SerializeScope(AIPromptScope scope)
    {
        return JsonSerializer.Serialize(scope, CoreConstants.DefaultJsonSerializerOptions);
    }

    private static AIPromptScope? DeserializeScope(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AIPromptScope>(json, CoreConstants.DefaultJsonSerializerOptions);
        }
        catch
        {
            return null;
        }
    }
}
