using System.Text.Json;
using Umbraco.Ai.Core.Versioning;

using CoreConstants = Umbraco.Ai.Core.Constants;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Versionable entity adapter for AI prompts.
/// </summary>
internal sealed class AiPromptVersionableEntityAdapter : AiVersionableEntityAdapterBase<AiPrompt>
{
    private readonly IAiPromptService _promptService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiPromptVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="promptService">The prompt service for rollback operations.</param>
    public AiPromptVersionableEntityAdapter(IAiPromptService promptService)
    {
        _promptService = promptService;
    }

    /// <inheritdoc />
    public override string EntityTypeName => "Prompt";

    /// <inheritdoc />
    protected override string CreateSnapshot(AiPrompt entity)
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
    protected override AiPrompt? RestoreFromSnapshotCore(string json)
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

            AiPromptScope? scope = null;
            if (root.TryGetProperty("scope", out var scopeElement) &&
                scopeElement.ValueKind == JsonValueKind.String)
            {
                var scopeJson = scopeElement.GetString();
                if (!string.IsNullOrEmpty(scopeJson))
                {
                    scope = DeserializeScope(scopeJson);
                }
            }

            return new AiPrompt
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
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null
                    ? cbu.GetInt32() : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null
                    ? mbu.GetInt32() : null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<AiPropertyChange> CompareVersions(AiPrompt from, AiPrompt to)
    {
        var changes = new List<AiPropertyChange>();

        if (from.Alias != to.Alias)
        {
            changes.Add(new AiPropertyChange("Alias", from.Alias, to.Alias));
        }

        if (from.Name != to.Name)
        {
            changes.Add(new AiPropertyChange("Name", from.Name, to.Name));
        }

        if (from.Description != to.Description)
        {
            changes.Add(new AiPropertyChange("Description", from.Description ?? "(empty)", to.Description ?? "(empty)"));
        }

        if (from.Instructions != to.Instructions)
        {
            changes.Add(new AiPropertyChange("Instructions", "(modified)", "(modified)"));
        }

        if (from.ProfileId != to.ProfileId)
        {
            changes.Add(new AiPropertyChange("ProfileId", from.ProfileId?.ToString() ?? "(none)", to.ProfileId?.ToString() ?? "(none)"));
        }

        // Compare context IDs
        var fromContextIds = string.Join(",", from.ContextIds);
        var toContextIds = string.Join(",", to.ContextIds);
        if (fromContextIds != toContextIds)
        {
            changes.Add(new AiPropertyChange("ContextIds", fromContextIds.Length > 0 ? fromContextIds : "(none)", toContextIds.Length > 0 ? toContextIds : "(none)"));
        }

        // Compare tags
        var fromTags = string.Join(",", from.Tags);
        var toTags = string.Join(",", to.Tags);
        if (fromTags != toTags)
        {
            changes.Add(new AiPropertyChange("Tags", fromTags.Length > 0 ? fromTags : "(none)", toTags.Length > 0 ? toTags : "(none)"));
        }

        if (from.IsActive != to.IsActive)
        {
            changes.Add(new AiPropertyChange("IsActive", from.IsActive.ToString(), to.IsActive.ToString()));
        }

        if (from.IncludeEntityContext != to.IncludeEntityContext)
        {
            changes.Add(new AiPropertyChange("IncludeEntityContext", from.IncludeEntityContext.ToString(), to.IncludeEntityContext.ToString()));
        }

        // Compare scope - indicate if changed
        var fromScopeHash = from.Scope?.GetHashCode().ToString() ?? "null";
        var toScopeHash = to.Scope?.GetHashCode().ToString() ?? "null";
        if (fromScopeHash != toScopeHash)
        {
            changes.Add(new AiPropertyChange("Scope", "(modified)", "(modified)"));
        }

        return changes;
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _promptService.RollbackPromptAsync(entityId, version, cancellationToken);

    private static string SerializeScope(AiPromptScope scope)
    {
        return JsonSerializer.Serialize(scope, CoreConstants.DefaultJsonSerializerOptions);
    }

    private static AiPromptScope? DeserializeScope(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AiPromptScope>(json, CoreConstants.DefaultJsonSerializerOptions);
        }
        catch
        {
            return null;
        }
    }
}
