using System.Text.Json;
using Umbraco.Ai.Core.Versioning;

namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Versionable entity adapter for AI contexts.
/// </summary>
internal sealed class AiContextVersionableEntityAdapter : AiVersionableEntityAdapterBase<AiContext>
{
    private readonly IAiContextService _contextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="contextService">The context service for rollback operations.</param>
    public AiContextVersionableEntityAdapter(IAiContextService contextService)
    {
        _contextService = contextService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AiContext entity)
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
    protected override AiContext? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var resources = new List<AiContextResource>();
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

                    resources.Add(new AiContextResource
                    {
                        Id = resourceElement.GetProperty("id").GetGuid(),
                        ResourceTypeId = resourceElement.GetProperty("resourceTypeId").GetString()!,
                        Name = resourceElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                            ? nameEl.GetString() : null,
                        Description = resourceElement.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                            ? descEl.GetString() : null,
                        SortOrder = resourceElement.GetProperty("sortOrder").GetInt32(),
                        Data = data,
                        InjectionMode = (AiContextResourceInjectionMode)resourceElement.GetProperty("injectionMode").GetInt32()
                    });
                }
            }

            return new AiContext
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
    protected override IReadOnlyList<AiPropertyChange> CompareVersions(AiContext from, AiContext to)
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

        // Compare resources count
        if (from.Resources.Count != to.Resources.Count)
        {
            changes.Add(new AiPropertyChange("Resources.Count", from.Resources.Count.ToString(), to.Resources.Count.ToString()));
        }

        // Track added resources
        var fromResourceIds = from.Resources.Select(r => r.Id).ToHashSet();
        var toResourceIds = to.Resources.Select(r => r.Id).ToHashSet();

        var addedIds = toResourceIds.Except(fromResourceIds);
        var removedIds = fromResourceIds.Except(toResourceIds);

        foreach (var addedId in addedIds)
        {
            var resource = to.Resources.First(r => r.Id == addedId);
            changes.Add(new AiPropertyChange($"Resources[{resource.Name ?? resource.ResourceTypeId}]", null, "Added"));
        }

        foreach (var removedId in removedIds)
        {
            var resource = from.Resources.First(r => r.Id == removedId);
            changes.Add(new AiPropertyChange($"Resources[{resource.Name ?? resource.ResourceTypeId}]", "Removed", null));
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

    private static IReadOnlyList<AiPropertyChange> CompareResources(AiContextResource from, AiContextResource to)
    {
        var changes = new List<AiPropertyChange>();
        var prefix = $"Resources[{from.Name ?? from.ResourceTypeId}]";

        if (from.Name != to.Name)
        {
            changes.Add(new AiPropertyChange($"{prefix}.Name", from.Name, to.Name));
        }

        if (from.Description != to.Description)
        {
            changes.Add(new AiPropertyChange($"{prefix}.Description", from.Description ?? "(empty)", to.Description ?? "(empty)"));
        }

        if (from.ResourceTypeId != to.ResourceTypeId)
        {
            changes.Add(new AiPropertyChange($"{prefix}.ResourceTypeId", from.ResourceTypeId, to.ResourceTypeId));
        }

        if (from.SortOrder != to.SortOrder)
        {
            changes.Add(new AiPropertyChange($"{prefix}.SortOrder", from.SortOrder.ToString(), to.SortOrder.ToString()));
        }

        if (from.InjectionMode != to.InjectionMode)
        {
            changes.Add(new AiPropertyChange($"{prefix}.InjectionMode", from.InjectionMode.ToString(), to.InjectionMode.ToString()));
        }

        // Compare data - just indicate if changed
        var fromDataHash = from.Data?.GetHashCode().ToString() ?? "null";
        var toDataHash = to.Data?.GetHashCode().ToString() ?? "null";
        if (fromDataHash != toDataHash)
        {
            changes.Add(new AiPropertyChange($"{prefix}.Data", "(modified)", "(modified)"));
        }

        return changes;
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _contextService.RollbackContextAsync(entityId, version, cancellationToken);

    /// <inheritdoc />
    protected override Task<AiContext?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _contextService.GetContextAsync(entityId, cancellationToken);
}
