using System.Text.Json;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;

namespace Umbraco.AI.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AIGuardrailRule"/> instances in tests.
/// </summary>
public class AIGuardrailRuleBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _evaluatorId = "pii";
    private string _name = "Test Rule";
    private AIGuardrailPhase _phase = AIGuardrailPhase.PostGenerate;
    private AIGuardrailAction _action = AIGuardrailAction.Block;
    private JsonElement? _config;
    private int _sortOrder;

    public AIGuardrailRuleBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AIGuardrailRuleBuilder WithEvaluatorId(string evaluatorId)
    {
        _evaluatorId = evaluatorId;
        return this;
    }

    public AIGuardrailRuleBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AIGuardrailRuleBuilder WithPhase(AIGuardrailPhase phase)
    {
        _phase = phase;
        return this;
    }

    public AIGuardrailRuleBuilder WithAction(AIGuardrailAction action)
    {
        _action = action;
        return this;
    }

    public AIGuardrailRuleBuilder WithConfig(JsonElement? config)
    {
        _config = config;
        return this;
    }

    public AIGuardrailRuleBuilder WithConfig(object config)
    {
        _config = JsonSerializer.SerializeToElement(config);
        return this;
    }

    public AIGuardrailRuleBuilder WithSortOrder(int sortOrder)
    {
        _sortOrder = sortOrder;
        return this;
    }

    public AIGuardrailRuleBuilder AsPreGenerate()
    {
        _phase = AIGuardrailPhase.PreGenerate;
        return this;
    }

    public AIGuardrailRuleBuilder AsPostGenerate()
    {
        _phase = AIGuardrailPhase.PostGenerate;
        return this;
    }

    public AIGuardrailRuleBuilder AsWarn()
    {
        _action = AIGuardrailAction.Warn;
        return this;
    }

    public AIGuardrailRuleBuilder AsBlock()
    {
        _action = AIGuardrailAction.Block;
        return this;
    }

    public AIGuardrailRule Build()
    {
        return new AIGuardrailRule
        {
            Id = _id,
            EvaluatorId = _evaluatorId,
            Name = _name,
            Phase = _phase,
            Action = _action,
            Config = _config,
            SortOrder = _sortOrder
        };
    }

    public static implicit operator AIGuardrailRule(AIGuardrailRuleBuilder builder) => builder.Build();
}
