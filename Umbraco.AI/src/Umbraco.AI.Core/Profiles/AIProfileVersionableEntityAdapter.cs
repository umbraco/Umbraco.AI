using System.Text.Json;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Versionable entity adapter for AI profiles.
/// </summary>
internal sealed class AIProfileVersionableEntityAdapter : AIVersionableEntityAdapterBase<AIProfile>
{
    private readonly IAIProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="profileService">The profile service for rollback operations.</param>
    public AIProfileVersionableEntityAdapter(IAIProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AIProfile entity)
    {
        var snapshot = new
        {
            entity.Id,
            entity.Alias,
            entity.Name,
            Capability = (int)entity.Capability,
            ProviderId = entity.Model.ProviderId,
            ModelId = entity.Model.ModelId,
            entity.ConnectionId,
            Settings = AIProfileSettingsSerializer.Serialize(entity.Settings),
            Tags = entity.Tags.Count > 0 ? string.Join(',', entity.Tags) : null,
            entity.Version,
            entity.DateCreated,
            entity.DateModified,
            entity.CreatedByUserId,
            entity.ModifiedByUserId
        };

        return JsonSerializer.Serialize(snapshot, Constants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    protected override AIProfile? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var capability = (AICapability)root.GetProperty("capability").GetInt32();

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

            IAIProfileSettings? settings = null;
            if (root.TryGetProperty("settings", out var settingsElement) &&
                settingsElement.ValueKind == JsonValueKind.String)
            {
                var settingsJson = settingsElement.GetString();
                if (!string.IsNullOrEmpty(settingsJson))
                {
                    settings = AIProfileSettingsSerializer.Deserialize(capability, settingsJson);
                }
            }

            return new AIProfile
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                Capability = capability,
                Model = new AIModelRef(
                    root.GetProperty("providerId").GetString()!,
                    root.GetProperty("modelId").GetString()!),
                ConnectionId = root.GetProperty("connectionId").GetGuid(),
                Settings = settings,
                Tags = tags,
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
    protected override IReadOnlyList<AIPropertyChange> CompareVersions(AIProfile from, AIProfile to)
    {
        var changes = new List<AIPropertyChange>();

        if (from.Alias != to.Alias)
        {
            changes.Add(new AIPropertyChange("Alias", from.Alias, to.Alias));
        }

        if (from.Name != to.Name)
        {
            changes.Add(new AIPropertyChange("Name", from.Name, to.Name));
        }

        if (from.Capability != to.Capability)
        {
            changes.Add(new AIPropertyChange("Capability", from.Capability.ToString(), to.Capability.ToString()));
        }

        if (from.Model.ProviderId != to.Model.ProviderId)
        {
            changes.Add(new AIPropertyChange("ProviderId", from.Model.ProviderId, to.Model.ProviderId));
        }

        if (from.Model.ModelId != to.Model.ModelId)
        {
            changes.Add(new AIPropertyChange("ModelId", from.Model.ModelId, to.Model.ModelId));
        }

        if (from.ConnectionId != to.ConnectionId)
        {
            changes.Add(new AIPropertyChange("ConnectionId", from.ConnectionId.ToString(), to.ConnectionId.ToString()));
        }

        // Compare tags
        var fromTags = string.Join(",", from.Tags);
        var toTags = string.Join(",", to.Tags);
        if (fromTags != toTags)
        {
            changes.Add(new AIPropertyChange("Tags", fromTags, toTags));
        }

        // Compare settings with deep inspection for known types
        CompareSettings(from.Settings, to.Settings, changes);

        return changes;
    }

    /// <summary>
    /// Compares profile settings with deep inspection for known types.
    /// </summary>
    private static void CompareSettings(IAIProfileSettings? from, IAIProfileSettings? to, List<AIPropertyChange> changes)
    {
        // Handle null cases
        if (from == null && to == null)
        {
            return;
        }

        if (from == null)
        {
            changes.Add(new AIPropertyChange("Settings", null, to!.GetType().Name));
            return;
        }

        if (to == null)
        {
            changes.Add(new AIPropertyChange("Settings", from.GetType().Name, null));
            return;
        }

        // Handle type mismatch
        if (from.GetType() != to.GetType())
        {
            changes.Add(new AIPropertyChange("Settings.Type", from.GetType().Name, to.GetType().Name));
            return;
        }

        // Deep compare based on settings type
        switch (from)
        {
            case AIChatProfileSettings chatFrom when to is AIChatProfileSettings chatTo:
                CompareChatSettings(chatFrom, chatTo, changes);
                break;

            case AIEmbeddingProfileSettings embeddingFrom when to is AIEmbeddingProfileSettings embeddingTo:
                CompareEmbeddingSettings(embeddingFrom, embeddingTo, changes);
                break;
        }
    }

    /// <summary>
    /// Compares chat profile settings properties.
    /// </summary>
    private static void CompareChatSettings(AIChatProfileSettings from, AIChatProfileSettings to, List<AIPropertyChange> changes)
    {
        if (from.Temperature != to.Temperature)
        {
            changes.Add(new AIPropertyChange(
                "Settings.Temperature",
                from.Temperature?.ToString() ?? "null",
                to.Temperature?.ToString() ?? "null"));
        }

        if (from.MaxTokens != to.MaxTokens)
        {
            changes.Add(new AIPropertyChange(
                "Settings.MaxTokens",
                from.MaxTokens?.ToString() ?? "null",
                to.MaxTokens?.ToString() ?? "null"));
        }

        if (from.SystemPromptTemplate != to.SystemPromptTemplate)
        {
            // Truncate long prompts for readability
            changes.Add(new AIPropertyChange(
                "Settings.SystemPromptTemplate",
                AIJsonComparer.TruncateValue(from.SystemPromptTemplate),
                AIJsonComparer.TruncateValue(to.SystemPromptTemplate)));
        }

        // Compare ContextIds collection
        CompareContextIds(from.ContextIds, to.ContextIds, changes);
    }

    /// <summary>
    /// Compares context ID collections and reports specific changes.
    /// </summary>
    private static void CompareContextIds(IReadOnlyList<Guid> from, IReadOnlyList<Guid> to, List<AIPropertyChange> changes)
    {
        var fromSet = new HashSet<Guid>(from);
        var toSet = new HashSet<Guid>(to);

        // Check if collections are equal
        if (fromSet.SetEquals(toSet) && from.Count == to.Count)
        {
            // Collections are identical (same items, same count)
            // Check if order changed
            var orderChanged = !from.SequenceEqual(to);
            if (orderChanged)
            {
                changes.Add(new AIPropertyChange(
                    "Settings.ContextIds",
                    string.Join(", ", from),
                    string.Join(", ", to)));
            }

            return;
        }

        // Report added and removed items
        var added = toSet.Except(fromSet).ToList();
        var removed = fromSet.Except(toSet).ToList();

        if (removed.Count > 0)
        {
            changes.Add(new AIPropertyChange(
                "Settings.ContextIds.Removed",
                string.Join(", ", removed),
                null));
        }

        if (added.Count > 0)
        {
            changes.Add(new AIPropertyChange(
                "Settings.ContextIds.Added",
                null,
                string.Join(", ", added)));
        }
    }

    /// <summary>
    /// Compares embedding profile settings properties.
    /// </summary>
    private static void CompareEmbeddingSettings(AIEmbeddingProfileSettings from, AIEmbeddingProfileSettings to, List<AIPropertyChange> changes)
    {
        // AIEmbeddingProfileSettings is currently empty
        // Add comparisons here when properties are added
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _profileService.RollbackProfileAsync(entityId, version, cancellationToken);

    /// <inheritdoc />
    protected override Task<AIProfile?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _profileService.GetProfileAsync(entityId, cancellationToken);
}
