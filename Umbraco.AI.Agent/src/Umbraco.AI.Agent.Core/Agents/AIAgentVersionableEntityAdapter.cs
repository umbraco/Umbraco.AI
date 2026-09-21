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
            entity.AgentType,
            entity.ProfileId,
            entity.GuardrailIds,
            SurfaceIds = entity.SurfaceIds.Count > 0 ? string.Join(',', entity.SurfaceIds) : null,
            entity.Scope,
            Config = AIAgentConfigSerializer.Serialize(entity.Config),
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

            var agentType = AIAgentType.Standard;
            if (root.TryGetProperty("agentType", out var atEl))
            {
                if (atEl.ValueKind == JsonValueKind.String)
                {
                    Enum.TryParse(atEl.GetString(), ignoreCase: true, out agentType);
                }
                else if (atEl.ValueKind == JsonValueKind.Number)
                {
                    agentType = (AIAgentType)atEl.GetInt32();
                }
            }

            string? configJson = root.TryGetProperty("config", out var configEl) && configEl.ValueKind == JsonValueKind.String
                ? configEl.GetString()
                : null;

            IReadOnlyList<Guid> guardrailIds = Array.Empty<Guid>();
            if (root.TryGetProperty("guardrailIds", out var guardrailIdsElement) &&
                guardrailIdsElement.ValueKind == JsonValueKind.Array)
            {
                guardrailIds = guardrailIdsElement.Deserialize<IReadOnlyList<Guid>>(CoreConstants.DefaultJsonSerializerOptions)
                    ?? Array.Empty<Guid>();
            }

            AIAgentScope? scope = root.TryGetProperty("scope", out var scopeElement) && scopeElement.ValueKind == JsonValueKind.Object
                ? scopeElement.Deserialize<AIAgentScope>(CoreConstants.DefaultJsonSerializerOptions)
                : null;

            return new AIAgent
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                Description = root.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                    ? descEl.GetString() : null,
                AgentType = agentType,
                ProfileId = root.GetProperty("profileId").GetGuid(),
                GuardrailIds = guardrailIds,
                SurfaceIds = surfaceIds,
                Scope = scope,
                Config = AIAgentConfigSerializer.Deserialize(agentType, configJson),
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

        // Compare surface IDs
        var fromSurfaceIds = string.Join(",", from.SurfaceIds);
        var toSurfaceIds = string.Join(",", to.SurfaceIds);
        if (fromSurfaceIds != toSurfaceIds)
        {
            changes.Add(new AIValueChange("SurfaceIds", fromSurfaceIds.Length > 0 ? fromSurfaceIds : "(none)", toSurfaceIds.Length > 0 ? toSurfaceIds : "(none)"));
        }

        // Compare guardrail IDs
        var fromGuardrailIds = string.Join(",", from.GuardrailIds);
        var toGuardrailIds = string.Join(",", to.GuardrailIds);
        if (fromGuardrailIds != toGuardrailIds)
        {
            changes.Add(new AIValueChange("GuardrailIds", fromGuardrailIds.Length > 0 ? fromGuardrailIds : "(none)", toGuardrailIds.Length > 0 ? toGuardrailIds : "(none)"));
        }

        // Compare scope (serialized for a stable, order-sensitive comparison of allow/deny rules)
        var fromScope = JsonSerializer.Serialize(from.Scope, CoreConstants.DefaultJsonSerializerOptions);
        var toScope = JsonSerializer.Serialize(to.Scope, CoreConstants.DefaultJsonSerializerOptions);
        if (fromScope != toScope)
        {
            changes.Add(new AIValueChange("Scope", from.Scope is null ? "(none)" : "(modified)", to.Scope is null ? "(none)" : "(modified)"));
        }

        if (from.AgentType != to.AgentType)
        {
            changes.Add(new AIValueChange("AgentType", from.AgentType.ToString(), to.AgentType.ToString()));
        }

        // Compare serialized config blobs
        var fromConfig = AIAgentConfigSerializer.Serialize(from.Config);
        var toConfig = AIAgentConfigSerializer.Serialize(to.Config);
        if (fromConfig != toConfig)
        {
            changes.Add(new AIValueChange("Config", "(modified)", "(modified)"));
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
