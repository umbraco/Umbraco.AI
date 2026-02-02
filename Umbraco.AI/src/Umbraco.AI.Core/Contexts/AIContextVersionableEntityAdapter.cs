using System.Text.Json;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Versionable entity adapter for AI contexts.
/// </summary>
internal sealed class AIContextVersionableEntityAdapter : AIVersionableEntityAdapterBase<AIContext>
{
    private readonly IAiContextService _contextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="contextService">The context service for rollback operations.</param>
    public AIContextVersionableEntityAdapter(IAiContextService contextService)
    {
        _contextService = contextService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AIContext entity)
    {
        var snapshot = new
        {
            entity.Id,
            entity.Alias,
            entity.Name,
            entity.DateCreated,
            entity.DateModified,
            entity.CreatedByUserId,
            entity.ModifiedByUserId,
            entity.Version,
            Resources = entity.Resources.Select(r => new
            {
                r.Id,
                r.ResourceTypeId,
                r.Name,
                r.Description,
                r.SortOrder,
                Data = r.Data is null ? null : JsonSerializer.Serialize(r.Data, Constants.DefaultJsonSerializerOptions),
                InjectionMode = (int)r.InjectionMode
            }).ToList()
        };

        return JsonSerializer.Serialize(snapshot, Constants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    protected override AIContext? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var resources = new List<AIContextResource>();
            if (root.TryGetProperty("resources", out var resourcesElement) &&
                resourcesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var resourceElement in resourcesElement.EnumerateArray())
                {
                    object? data = null;
                    if (resourceElement.TryGetProperty("data", out var dataElement) &&
                        dataElement.ValueKind == JsonValueKind.String)
                    {
                        var dataJson = dataElement.GetString();
                        if (!string.IsNullOrEmpty(dataJson))
                        {
                            data = JsonSerializer.Deserialize<JsonElement>(dataJson, Constants.DefaultJsonSerializerOptions);
                        }
                    }

                    resources.Add(new AIContextResource
                    {
                        Id = resourceElement.GetProperty("id").GetGuid(),
                        ResourceTypeId = resourceElement.GetProperty("resourceTypeId").GetString()!,
                        Name = resourceElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                            ? nameEl.GetString() : null,
                        Description = resourceElement.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                            ? descEl.GetString() : null,
                        SortOrder = resourceElement.GetProperty("sortOrder").GetInt32(),
                        Data = data,
                        InjectionMode = (AIContextResourceInjectionMode)resourceElement.GetProperty("injectionMode").GetInt32()
                    });
                }
            }

            return new AIContext
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                DateCreated = root.GetProperty("dateCreated").GetDateTime(),
                DateModified = root.GetProperty("dateModified").GetDateTime(),
                // Try Guid first (new format), ignore old int values (no conversion path)
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null && cbu.TryGetGuid(out var cbuGuid)
                    ? cbuGuid : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null && mbu.TryGetGuid(out var mbuGuid)
                    ? mbuGuid : null,
                Version = root.GetProperty("version").GetInt32(),
                Resources = resources
            };
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<AIPropertyChange> CompareVersions(AIContext from, AIContext to)
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

        // Compare resources count
        if (from.Resources.Count != to.Resources.Count)
        {
            changes.Add(new AIPropertyChange("Resources.Count", from.Resources.Count.ToString(), to.Resources.Count.ToString()));
        }

        // Track added resources
        var fromResourceIds = from.Resources.Select(r => r.Id).ToHashSet();
        var toResourceIds = to.Resources.Select(r => r.Id).ToHashSet();

        var addedIds = toResourceIds.Except(fromResourceIds);
        var removedIds = fromResourceIds.Except(toResourceIds);

        foreach (var addedId in addedIds)
        {
            var resource = to.Resources.First(r => r.Id == addedId);
            changes.Add(new AIPropertyChange($"Resources[{resource.Name ?? resource.ResourceTypeId}]", null, "Added"));
        }

        foreach (var removedId in removedIds)
        {
            var resource = from.Resources.First(r => r.Id == removedId);
            changes.Add(new AIPropertyChange($"Resources[{resource.Name ?? resource.ResourceTypeId}]", "Removed", null));
        }

        // Compare existing resources
        var commonIds = fromResourceIds.Intersect(toResourceIds);
        foreach (var id in commonIds)
        {
            var fromResource = from.Resources.First(r => r.Id == id);
            var toResource = to.Resources.First(r => r.Id == id);

            var resourceChanges = CompareResources(fromResource, toResource);
            changes.AddRange(resourceChanges);
        }

        return changes;
    }

    private static IReadOnlyList<AIPropertyChange> CompareResources(AIContextResource from, AIContextResource to)
    {
        var changes = new List<AIPropertyChange>();
        var prefix = $"Resources[{from.Name ?? from.ResourceTypeId}]";

        if (from.Name != to.Name)
        {
            changes.Add(new AIPropertyChange($"{prefix}.Name", from.Name, to.Name));
        }

        if (from.Description != to.Description)
        {
            changes.Add(new AIPropertyChange($"{prefix}.Description", from.Description ?? "(empty)", to.Description ?? "(empty)"));
        }

        if (from.ResourceTypeId != to.ResourceTypeId)
        {
            changes.Add(new AIPropertyChange($"{prefix}.ResourceTypeId", from.ResourceTypeId, to.ResourceTypeId));
        }

        if (from.SortOrder != to.SortOrder)
        {
            changes.Add(new AIPropertyChange($"{prefix}.SortOrder", from.SortOrder.ToString(), to.SortOrder.ToString()));
        }

        if (from.InjectionMode != to.InjectionMode)
        {
            changes.Add(new AIPropertyChange($"{prefix}.InjectionMode", from.InjectionMode.ToString(), to.InjectionMode.ToString()));
        }

        // Compare data with deep inspection using shared utility
        var success = AIJsonComparer.CompareObjects(from.Data, to.Data, $"{prefix}.Data", changes);

        if (!success && !Equals(from.Data, to.Data))
        {
            // Fallback if comparison failed
            changes.Add(new AIPropertyChange($"{prefix}.Data", "(modified)", "(modified)"));
        }

        return changes;
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _contextService.RollbackContextAsync(entityId, version, cancellationToken);

    /// <inheritdoc />
    protected override Task<AIContext?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _contextService.GetContextAsync(entityId, cancellationToken);
}
