using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;

namespace Umbraco.AI.Persistence.Guardrails;

/// <summary>
/// Factory for mapping between <see cref="AIGuardrail"/> domain models and <see cref="AIGuardrailEntity"/> database entities.
/// Handles encryption/decryption of sensitive evaluator configuration during the mapping process.
/// </summary>
internal sealed class AIGuardrailFactory : IAIGuardrailFactory
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AIGuardrailEvaluatorCollection _evaluators;

    public AIGuardrailFactory(
        IAIEditableModelSerializer serializer,
        AIGuardrailEvaluatorCollection evaluators)
    {
        _serializer = serializer;
        _evaluators = evaluators;
    }

    /// <inheritdoc />
    public AIGuardrail BuildDomain(AIGuardrailEntity entity)
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

    private AIGuardrailRule BuildRuleDomain(AIGuardrailRuleEntity entity)
    {
        // Config is stored as JSON with sensitive fields encrypted. The serializer decrypts any
        // encrypted values; typed deserialization (and configuration variable resolution) happens
        // later when the evaluator runs.
        var config = _serializer.Deserialize(entity.Config);

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

    /// <inheritdoc />
    public AIGuardrailEntity BuildEntity(AIGuardrail guardrail)
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

    /// <inheritdoc />
    public AIGuardrailRuleEntity BuildRuleEntity(AIGuardrailRule rule, Guid guardrailId)
    {
        return new AIGuardrailRuleEntity
        {
            Id = rule.Id,
            GuardrailId = guardrailId,
            EvaluatorId = rule.EvaluatorId,
            Name = rule.Name,
            Phase = (int)rule.Phase,
            Action = (int)rule.Action,
            Config = SerializeConfig(rule),
            SortOrder = rule.SortOrder,
        };
    }

    /// <inheritdoc />
    public void UpdateEntity(AIGuardrailEntity entity, AIGuardrail guardrail)
    {
        entity.Alias = guardrail.Alias;
        entity.Name = guardrail.Name;
        entity.DateModified = guardrail.DateModified;
        entity.ModifiedByUserId = guardrail.ModifiedByUserId;
        entity.Version = guardrail.Version;
    }

    /// <inheritdoc />
    public void UpdateRuleEntity(AIGuardrailRuleEntity entity, AIGuardrailRule rule)
    {
        entity.EvaluatorId = rule.EvaluatorId;
        entity.Name = rule.Name;
        entity.Phase = (int)rule.Phase;
        entity.Action = (int)rule.Action;
        entity.Config = SerializeConfig(rule);
        entity.SortOrder = rule.SortOrder;
    }

    /// <summary>
    /// Serializes a rule's configuration, encrypting sensitive fields based on the evaluator schema.
    /// </summary>
    private string? SerializeConfig(AIGuardrailRule rule)
    {
        if (rule.Config is null)
        {
            return null;
        }

        var schema = GetConfigSchema(rule.EvaluatorId);
        return _serializer.Serialize(rule.Config, schema);
    }

    private AIEditableModelSchema? GetConfigSchema(string evaluatorId)
        => _evaluators.GetById(evaluatorId)?.GetConfigSchema();
}
