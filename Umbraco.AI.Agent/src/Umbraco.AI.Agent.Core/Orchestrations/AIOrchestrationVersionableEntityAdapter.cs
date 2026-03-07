using System.Text.Json;
using Umbraco.AI.Core.Versioning;

using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Versionable entity adapter for AI orchestrations.
/// </summary>
internal sealed class AIOrchestrationVersionableEntityAdapter : AIVersionableEntityAdapterBase<AIOrchestration>
{
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly IAIEntityVersionService _versionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIOrchestrationVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="orchestrationService">The orchestration service for save operations.</param>
    /// <param name="versionService">The entity version service for retrieving snapshots.</param>
    public AIOrchestrationVersionableEntityAdapter(IAIOrchestrationService orchestrationService, IAIEntityVersionService versionService)
    {
        _orchestrationService = orchestrationService;
        _versionService = versionService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AIOrchestration entity)
    {
        var snapshot = new
        {
            entity.Id,
            entity.Alias,
            entity.Name,
            entity.Description,
            entity.ProfileId,
            SurfaceIds = entity.SurfaceIds.Count > 0 ? string.Join(',', entity.SurfaceIds) : null,
            Graph = JsonSerializer.Serialize(entity.Graph, CoreConstants.DefaultJsonSerializerOptions),
            entity.IsActive,
            entity.Version,
            entity.DateCreated,
            entity.DateModified,
            entity.CreatedByUserId,
            entity.ModifiedByUserId
        };

        return JsonSerializer.Serialize(snapshot, CoreConstants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    protected override AIOrchestration? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            IReadOnlyList<string> surfaceIds = Array.Empty<string>();
            if (root.TryGetProperty("surfaceIds", out var surfaceIdsElement) &&
                surfaceIdsElement.ValueKind == JsonValueKind.String)
            {
                var surfaceIdsString = surfaceIdsElement.GetString();
                if (!string.IsNullOrEmpty(surfaceIdsString))
                {
                    surfaceIds = surfaceIdsString
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
                }
            }

            var graph = new AIOrchestrationGraph();
            if (root.TryGetProperty("graph", out var graphElement) &&
                graphElement.ValueKind == JsonValueKind.String)
            {
                var graphJson = graphElement.GetString();
                if (!string.IsNullOrEmpty(graphJson))
                {
                    graph = JsonSerializer.Deserialize<AIOrchestrationGraph>(graphJson, CoreConstants.DefaultJsonSerializerOptions)
                        ?? new AIOrchestrationGraph();
                }
            }

            return new AIOrchestration
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                Description = root.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                    ? descEl.GetString() : null,
                ProfileId = root.TryGetProperty("profileId", out var profEl) && profEl.ValueKind != JsonValueKind.Null
                    ? profEl.GetGuid() : null,
                SurfaceIds = surfaceIds,
                Graph = graph,
                IsActive = root.GetProperty("isActive").GetBoolean(),
                Version = root.GetProperty("version").GetInt32(),
                DateCreated = root.GetProperty("dateCreated").GetDateTime(),
                DateModified = root.GetProperty("dateModified").GetDateTime(),
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
    protected override IReadOnlyList<AIValueChange> CompareVersions(AIOrchestration from, AIOrchestration to)
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

        if (from.ProfileId != to.ProfileId)
        {
            changes.Add(new AIValueChange("ProfileId", from.ProfileId.ToString(), to.ProfileId.ToString()));
        }

        var fromSurfaceIds = string.Join(",", from.SurfaceIds);
        var toSurfaceIds = string.Join(",", to.SurfaceIds);
        if (fromSurfaceIds != toSurfaceIds)
        {
            changes.Add(new AIValueChange("SurfaceIds", fromSurfaceIds.Length > 0 ? fromSurfaceIds : "(none)", toSurfaceIds.Length > 0 ? toSurfaceIds : "(none)"));
        }

        // Compare graph by serialized JSON
        var fromGraph = JsonSerializer.Serialize(from.Graph, CoreConstants.DefaultJsonSerializerOptions);
        var toGraph = JsonSerializer.Serialize(to.Graph, CoreConstants.DefaultJsonSerializerOptions);
        if (fromGraph != toGraph)
        {
            changes.Add(new AIValueChange("Graph", "(modified)", "(modified)"));
        }

        if (from.IsActive != to.IsActive)
        {
            changes.Add(new AIValueChange("IsActive", from.IsActive.ToString(), to.IsActive.ToString()));
        }

        return changes;
    }

    /// <inheritdoc />
    public override async Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
    {
        var snapshot = await _versionService.GetVersionSnapshotAsync<AIOrchestration>(entityId, version, cancellationToken)
            ?? throw new InvalidOperationException($"Orchestration version {version} not found for orchestration {entityId}");

        // Save the snapshot as the current version (this will create a new version)
        await _orchestrationService.SaveOrchestrationAsync(snapshot, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<AIOrchestration?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _orchestrationService.GetOrchestrationAsync(entityId, cancellationToken);
}
