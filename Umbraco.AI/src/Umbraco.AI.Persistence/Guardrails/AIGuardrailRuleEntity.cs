namespace Umbraco.AI.Persistence.Guardrails;

/// <summary>
/// EF Core entity representing a rule within an AI guardrail.
/// </summary>
internal class AIGuardrailRuleEntity
{
    /// <summary>
    /// Unique identifier for the rule.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the parent guardrail.
    /// </summary>
    public Guid GuardrailId { get; set; }

    /// <summary>
    /// The identifier of the registered evaluator.
    /// </summary>
    public string EvaluatorId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The phase in which this rule is evaluated (0=PreGenerate, 1=PostGenerate).
    /// </summary>
    public int Phase { get; set; }

    /// <summary>
    /// The action to take when flagged (0=Block, 1=Warn).
    /// </summary>
    public int Action { get; set; }

    /// <summary>
    /// Evaluator-specific configuration as JSON.
    /// </summary>
    public string? Config { get; set; }

    /// <summary>
    /// Sort order within the guardrail.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Navigation property to the parent guardrail.
    /// </summary>
    public AIGuardrailEntity? Guardrail { get; set; }
}
