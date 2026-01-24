using System.Text.Json;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Versioning;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Versionable entity adapter for AI profiles.
/// </summary>
internal sealed class AiProfileVersionableEntityAdapter : AiVersionableEntityAdapterBase<AiProfile>
{
    private readonly IAiProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiProfileVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="profileService">The profile service for rollback operations.</param>
    public AiProfileVersionableEntityAdapter(IAiProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AiProfile entity)
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
            Settings = AiProfileSettingsSerializer.Serialize(entity.Settings),
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
    protected override AiProfile? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var capability = (AiCapability)root.GetProperty("capability").GetInt32();

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

            IAiProfileSettings? settings = null;
            if (root.TryGetProperty("settings", out var settingsElement) &&
                settingsElement.ValueKind == JsonValueKind.String)
            {
                var settingsJson = settingsElement.GetString();
                if (!string.IsNullOrEmpty(settingsJson))
                {
                    settings = AiProfileSettingsSerializer.Deserialize(capability, settingsJson);
                }
            }

            return new AiProfile
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                Capability = capability,
                Model = new AiModelRef(
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
    protected override IReadOnlyList<AiPropertyChange> CompareVersions(AiProfile from, AiProfile to)
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

        if (from.Capability != to.Capability)
        {
            changes.Add(new AiPropertyChange("Capability", from.Capability.ToString(), to.Capability.ToString()));
        }

        if (from.Model.ProviderId != to.Model.ProviderId)
        {
            changes.Add(new AiPropertyChange("ProviderId", from.Model.ProviderId, to.Model.ProviderId));
        }

        if (from.Model.ModelId != to.Model.ModelId)
        {
            changes.Add(new AiPropertyChange("ModelId", from.Model.ModelId, to.Model.ModelId));
        }

        if (from.ConnectionId != to.ConnectionId)
        {
            changes.Add(new AiPropertyChange("ConnectionId", from.ConnectionId.ToString(), to.ConnectionId.ToString()));
        }

        // Compare tags
        var fromTags = string.Join(",", from.Tags);
        var toTags = string.Join(",", to.Tags);
        if (fromTags != toTags)
        {
            changes.Add(new AiPropertyChange("Tags", fromTags, toTags));
        }

        // Compare settings with deep inspection for known types
        CompareSettings(from.Settings, to.Settings, changes);

        return changes;
    }

    /// <summary>
    /// Compares profile settings with deep inspection for known types.
    /// </summary>
    private static void CompareSettings(IAiProfileSettings? from, IAiProfileSettings? to, List<AiPropertyChange> changes)
    {
        // Handle null cases
        if (from == null && to == null)
        {
            return;
        }

        if (from == null)
        {
            changes.Add(new AiPropertyChange("Settings", null, to!.GetType().Name));
            return;
        }

        if (to == null)
        {
            changes.Add(new AiPropertyChange("Settings", from.GetType().Name, null));
            return;
        }

        // Handle type mismatch
        if (from.GetType() != to.GetType())
        {
            changes.Add(new AiPropertyChange("Settings.Type", from.GetType().Name, to.GetType().Name));
            return;
        }

        // Deep compare based on settings type
        switch (from)
        {
            case AiChatProfileSettings chatFrom when to is AiChatProfileSettings chatTo:
                CompareChatSettings(chatFrom, chatTo, changes);
                break;

            case AiEmbeddingProfileSettings embeddingFrom when to is AiEmbeddingProfileSettings embeddingTo:
                CompareEmbeddingSettings(embeddingFrom, embeddingTo, changes);
                break;
        }
    }

    /// <summary>
    /// Compares chat profile settings properties.
    /// </summary>
    private static void CompareChatSettings(AiChatProfileSettings from, AiChatProfileSettings to, List<AiPropertyChange> changes)
    {
        if (from.Temperature != to.Temperature)
        {
            changes.Add(new AiPropertyChange(
                "Settings.Temperature",
                from.Temperature?.ToString() ?? "null",
                to.Temperature?.ToString() ?? "null"));
        }

        if (from.MaxTokens != to.MaxTokens)
        {
            changes.Add(new AiPropertyChange(
                "Settings.MaxTokens",
                from.MaxTokens?.ToString() ?? "null",
                to.MaxTokens?.ToString() ?? "null"));
        }

        if (from.SystemPromptTemplate != to.SystemPromptTemplate)
        {
            // Truncate long prompts for readability
            changes.Add(new AiPropertyChange(
                "Settings.SystemPromptTemplate",
                TruncateValue(from.SystemPromptTemplate),
                TruncateValue(to.SystemPromptTemplate)));
        }

        // Compare ContextIds collection
        CompareContextIds(from.ContextIds, to.ContextIds, changes);
    }

    /// <summary>
    /// Compares context ID collections and reports specific changes.
    /// </summary>
    private static void CompareContextIds(IReadOnlyList<Guid> from, IReadOnlyList<Guid> to, List<AiPropertyChange> changes)
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
                changes.Add(new AiPropertyChange(
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
            changes.Add(new AiPropertyChange(
                "Settings.ContextIds.Removed",
                string.Join(", ", removed),
                null));
        }

        if (added.Count > 0)
        {
            changes.Add(new AiPropertyChange(
                "Settings.ContextIds.Added",
                null,
                string.Join(", ", added)));
        }
    }

    /// <summary>
    /// Compares embedding profile settings properties.
    /// </summary>
    private static void CompareEmbeddingSettings(AiEmbeddingProfileSettings from, AiEmbeddingProfileSettings to, List<AiPropertyChange> changes)
    {
        // AiEmbeddingProfileSettings is currently empty
        // Add comparisons here when properties are added
    }

    /// <summary>
    /// Truncates a value for display in change logs.
    /// </summary>
    private static string? TruncateValue(string? value, int maxLength = 100)
    {
        if (value == null)
        {
            return null;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...";
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _profileService.RollbackProfileAsync(entityId, version, cancellationToken);

    /// <inheritdoc />
    protected override Task<AiProfile?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _profileService.GetProfileAsync(entityId, cancellationToken);
}
