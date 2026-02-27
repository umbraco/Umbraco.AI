using System.Text.Json;
using Umbraco.AI.Core.Versioning;

using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Versionable entity adapter for AI agents.
/// </summary>
internal sealed class AIAgentVersionableEntityAdapter : AIVersionableEntityAdapterBase<AIAgent>
{
    private readonly IAIAgentService _agentService;
    private readonly IAIEntityVersionService _versionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="agentService">The agent service for save operations.</param>
    /// <param name="versionService">The entity version service for retrieving snapshots.</param>
    public AIAgentVersionableEntityAdapter(IAIAgentService agentService, IAIEntityVersionService versionService)
    {
        _agentService = agentService;
        _versionService = versionService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AIAgent entity)
    {
        var snapshot = new
        {
            entity.Id,
            entity.Alias,
            entity.Name,
            entity.Description,
            entity.ProfileId,
            ContextIds = entity.ContextIds.Count > 0 ? string.Join(',', entity.ContextIds) : null,
            SurfaceIds = entity.SurfaceIds.Count > 0 ? string.Join(',', entity.SurfaceIds) : null,
            entity.Instructions,
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
    protected override AIAgent? RestoreFromSnapshot(string json)
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

            return new AIAgent
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                Description = root.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                    ? descEl.GetString() : null,
                ProfileId = root.GetProperty("profileId").GetGuid(),
                ContextIds = contextIds,
                SurfaceIds = surfaceIds,
                Instructions = root.TryGetProperty("instructions", out var instrEl) && instrEl.ValueKind == JsonValueKind.String
                    ? instrEl.GetString() : null,
                IsActive = root.GetProperty("isActive").GetBoolean(),
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
    protected override IReadOnlyList<AIValueChange> CompareVersions(AIAgent from, AIAgent to)
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

        // Compare context IDs
        var fromContextIds = string.Join(",", from.ContextIds);
        var toContextIds = string.Join(",", to.ContextIds);
        if (fromContextIds != toContextIds)
        {
            changes.Add(new AIValueChange("ContextIds", fromContextIds.Length > 0 ? fromContextIds : "(none)", toContextIds.Length > 0 ? toContextIds : "(none)"));
        }

        // Compare scope IDs
        var fromSurfaceIds = string.Join(",", from.SurfaceIds);
        var toSurfaceIds = string.Join(",", to.SurfaceIds);
        if (fromSurfaceIds != toSurfaceIds)
        {
            changes.Add(new AIValueChange("SurfaceIds", fromSurfaceIds.Length > 0 ? fromSurfaceIds : "(none)", toSurfaceIds.Length > 0 ? toSurfaceIds : "(none)"));
        }

        if (from.Instructions != to.Instructions)
        {
            changes.Add(new AIValueChange("Instructions", "(modified)", "(modified)"));
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
        var snapshot = await _versionService.GetVersionSnapshotAsync<AIAgent>(entityId, version, cancellationToken)
            ?? throw new InvalidOperationException($"Agent version {version} not found for agent {entityId}");

        // Save the snapshot as the current version (this will create a new version)
        await _agentService.SaveAgentAsync(snapshot, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<AIAgent?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _agentService.GetAgentAsync(entityId, cancellationToken);
}
