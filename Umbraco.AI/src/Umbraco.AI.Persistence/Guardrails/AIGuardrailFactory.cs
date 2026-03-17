using System.Text.Json;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Guardrails;

namespace Umbraco.AI.Persistence.Guardrails;

/// <summary>
/// Factory for mapping between <see cref="AIGuardrail"/> domain models and <see cref="AIGuardrailEntity"/> database entities.
/// </summary>
internal static class AIGuardrailFactory
{
    public static AIGuardrail BuildDomain(AIGuardrailEntity entity)
    {
        return new AIGuardrail
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId,
            Version = entity.Version,
            Rules = entity.Rules
                .OrderBy(r => r.SortOrder)
                .Select(BuildRuleDomain)
                .ToList()
        };
    }

    public static AIGuardrailRule BuildRuleDomain(AIGuardrailRuleEntity entity)
    {
        JsonElement? config = null;
        if (!string.IsNullOrEmpty(entity.Config))
        {
            config = JsonSerializer.Deserialize<JsonElement>(entity.Config, Constants.DefaultJsonSerializerOptions);
        }

        return new AIGuardrailRule
        {
            Id = entity.Id,
            EvaluatorId = entity.EvaluatorId,
            Name = entity.Name,
            Phase = (AIGuardrailPhase)entity.Phase,
            Action = (AIGuardrailAction)entity.Action,
            Config = config,
            SortOrder = entity.SortOrder,
        };
    }

    public static AIGuardrailEntity BuildEntity(AIGuardrail guardrail)
    {
        return new AIGuardrailEntity
        {
            Id = guardrail.Id,
            Alias = guardrail.Alias,
            Name = guardrail.Name,
            DateCreated = guardrail.DateCreated,
            DateModified = guardrail.DateModified,
            CreatedByUserId = guardrail.CreatedByUserId,
            ModifiedByUserId = guardrail.ModifiedByUserId,
            Version = guardrail.Version,
            Rules = guardrail.Rules
                .Select(r => BuildRuleEntity(r, guardrail.Id))
                .ToList()
        };
    }

    public static AIGuardrailRuleEntity BuildRuleEntity(AIGuardrailRule rule, Guid guardrailId)
    {
        return new AIGuardrailRuleEntity
        {
            Id = rule.Id,
            GuardrailId = guardrailId,
            EvaluatorId = rule.EvaluatorId,
            Name = rule.Name,
            Phase = (int)rule.Phase,
            Action = (int)rule.Action,
            Config = rule.Config is null ? null : JsonSerializer.Serialize(rule.Config, Constants.DefaultJsonSerializerOptions),
            SortOrder = rule.SortOrder,
        };
    }

    public static void UpdateEntity(AIGuardrailEntity entity, AIGuardrail guardrail)
    {
        entity.Alias = guardrail.Alias;
        entity.Name = guardrail.Name;
        entity.DateModified = guardrail.DateModified;
        entity.ModifiedByUserId = guardrail.ModifiedByUserId;
        entity.Version = guardrail.Version;
    }

    public static void UpdateRuleEntity(AIGuardrailRuleEntity entity, AIGuardrailRule rule)
    {
        entity.EvaluatorId = rule.EvaluatorId;
        entity.Name = rule.Name;
        entity.Phase = (int)rule.Phase;
        entity.Action = (int)rule.Action;
        entity.Config = rule.Config is null ? null : JsonSerializer.Serialize(rule.Config, Constants.DefaultJsonSerializerOptions);
        entity.SortOrder = rule.SortOrder;
    }
}
