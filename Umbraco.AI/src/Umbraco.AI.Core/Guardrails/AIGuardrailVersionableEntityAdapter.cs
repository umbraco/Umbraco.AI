using System.Text.Json;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Versionable entity adapter for AI guardrails.
/// </summary>
internal sealed class AIGuardrailVersionableEntityAdapter : AIVersionableEntityAdapterBase<AIGuardrail>
{
    private readonly IAIGuardrailService _guardrailService;

    public AIGuardrailVersionableEntityAdapter(IAIGuardrailService guardrailService)
    {
        _guardrailService = guardrailService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AIGuardrail entity)
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
            Rules = entity.Rules.Select(r => new
            {
                r.Id,
                r.EvaluatorId,
                r.Name,
                Phase = (int)r.Phase,
                Action = (int)r.Action,
                Config = r.Config is null ? null : JsonSerializer.Serialize(r.Config, Constants.DefaultJsonSerializerOptions),
                r.SortOrder
            }).ToList()
        };

        return JsonSerializer.Serialize(snapshot, Constants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    protected override AIGuardrail? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var rules = new List<AIGuardrailRule>();
            if (root.TryGetProperty("rules", out var rulesElement) &&
                rulesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var ruleElement in rulesElement.EnumerateArray())
                {
                    JsonElement? config = null;
                    if (ruleElement.TryGetProperty("config", out var configElement) &&
                        configElement.ValueKind == JsonValueKind.String)
                    {
                        var configJson = configElement.GetString();
                        if (!string.IsNullOrEmpty(configJson))
                        {
                            config = JsonSerializer.Deserialize<JsonElement>(configJson, Constants.DefaultJsonSerializerOptions);
                        }
                    }

                    rules.Add(new AIGuardrailRule
                    {
                        Id = ruleElement.GetProperty("id").GetGuid(),
                        EvaluatorId = ruleElement.GetProperty("evaluatorId").GetString()!,
                        Name = ruleElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                            ? nameEl.GetString()! : string.Empty,
                        Phase = (AIGuardrailPhase)ruleElement.GetProperty("phase").GetInt32(),
                        Action = (AIGuardrailAction)ruleElement.GetProperty("action").GetInt32(),
                        Config = config,
                        SortOrder = ruleElement.GetProperty("sortOrder").GetInt32(),
                    });
                }
            }

            return new AIGuardrail
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                DateCreated = root.GetProperty("dateCreated").GetDateTime(),
                DateModified = root.GetProperty("dateModified").GetDateTime(),
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null && cbu.TryGetGuid(out var cbuGuid)
                    ? cbuGuid : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null && mbu.TryGetGuid(out var mbuGuid)
                    ? mbuGuid : null,
                Version = root.GetProperty("version").GetInt32(),
                Rules = rules
            };
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<AIValueChange> CompareVersions(AIGuardrail from, AIGuardrail to)
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

        if (from.Rules.Count != to.Rules.Count)
        {
            changes.Add(new AIValueChange("Rules.Count", from.Rules.Count.ToString(), to.Rules.Count.ToString()));
        }

        // Track added/removed rules
        var fromRuleIds = from.Rules.Select(r => r.Id).ToHashSet();
        var toRuleIds = to.Rules.Select(r => r.Id).ToHashSet();

        foreach (var addedId in toRuleIds.Except(fromRuleIds))
        {
            var rule = to.Rules.First(r => r.Id == addedId);
            changes.Add(new AIValueChange($"Rules[{rule.Name}]", null, "Added"));
        }

        foreach (var removedId in fromRuleIds.Except(toRuleIds))
        {
            var rule = from.Rules.First(r => r.Id == removedId);
            changes.Add(new AIValueChange($"Rules[{rule.Name}]", "Removed", null));
        }

        // Compare existing rules
        foreach (var id in fromRuleIds.Intersect(toRuleIds))
        {
            var fromRule = from.Rules.First(r => r.Id == id);
            var toRule = to.Rules.First(r => r.Id == id);
            var prefix = $"Rules[{fromRule.Name}]";

            if (fromRule.Name != toRule.Name)
            {
                changes.Add(new AIValueChange($"{prefix}.Name", fromRule.Name, toRule.Name));
            }

            if (fromRule.EvaluatorId != toRule.EvaluatorId)
            {
                changes.Add(new AIValueChange($"{prefix}.EvaluatorId", fromRule.EvaluatorId, toRule.EvaluatorId));
            }

            if (fromRule.Phase != toRule.Phase)
            {
                changes.Add(new AIValueChange($"{prefix}.Phase", fromRule.Phase.ToString(), toRule.Phase.ToString()));
            }

            if (fromRule.Action != toRule.Action)
            {
                changes.Add(new AIValueChange($"{prefix}.Action", fromRule.Action.ToString(), toRule.Action.ToString()));
            }

            if (fromRule.SortOrder != toRule.SortOrder)
            {
                changes.Add(new AIValueChange($"{prefix}.SortOrder", fromRule.SortOrder.ToString(), toRule.SortOrder.ToString()));
            }
        }

        return changes;
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _guardrailService.RollbackGuardrailAsync(entityId, version, cancellationToken);

    /// <inheritdoc />
    protected override Task<AIGuardrail?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _guardrailService.GetGuardrailAsync(entityId, cancellationToken);
}
