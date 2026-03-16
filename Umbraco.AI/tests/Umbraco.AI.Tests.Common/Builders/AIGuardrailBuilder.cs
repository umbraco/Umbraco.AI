using Umbraco.AI.Core.Guardrails;

namespace Umbraco.AI.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AIGuardrail"/> instances in tests.
/// </summary>
public class AIGuardrailBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alias = "test-guardrail";
    private string _name = "Test Guardrail";
    private DateTime _dateCreated = DateTime.UtcNow;
    private DateTime _dateModified = DateTime.UtcNow;
    private IList<AIGuardrailRule> _rules = new List<AIGuardrailRule>();

    public AIGuardrailBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AIGuardrailBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public AIGuardrailBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AIGuardrailBuilder WithDateCreated(DateTime dateCreated)
    {
        _dateCreated = dateCreated;
        return this;
    }

    public AIGuardrailBuilder WithDateModified(DateTime dateModified)
    {
        _dateModified = dateModified;
        return this;
    }

    public AIGuardrailBuilder WithRules(params AIGuardrailRule[] rules)
    {
        _rules = rules.ToList();
        return this;
    }

    public AIGuardrailBuilder WithRules(IEnumerable<AIGuardrailRule> rules)
    {
        _rules = rules.ToList();
        return this;
    }

    public AIGuardrailBuilder AddRule(AIGuardrailRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    public AIGuardrailBuilder AddRule(AIGuardrailRuleBuilder ruleBuilder)
    {
        _rules.Add(ruleBuilder.Build());
        return this;
    }

    public AIGuardrail Build()
    {
        return new AIGuardrail
        {
            Id = _id,
            Alias = _alias,
            Name = _name,
            DateCreated = _dateCreated,
            DateModified = _dateModified,
            Rules = _rules
        };
    }

    public static implicit operator AIGuardrail(AIGuardrailBuilder builder) => builder.Build();
}
