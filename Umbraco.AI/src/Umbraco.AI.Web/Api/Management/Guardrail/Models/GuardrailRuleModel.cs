using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Models;

/// <summary>
/// Model representing a rule within a guardrail.
/// </summary>
public class GuardrailRuleModel
{
    /// <summary>
    /// The unique identifier of the rule.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The identifier of the registered evaluator (e.g., "pii", "toxicity", "llm-judge").
    /// </summary>
    [Required]
    public string EvaluatorId { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the rule.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The phase in which this rule is evaluated (PreGenerate or PostGenerate).
    /// </summary>
    [Required]
    public string Phase { get; set; } = "PostGenerate";

    /// <summary>
    /// The action to take when this rule flags content (Block or Warn).
    /// </summary>
    [Required]
    public string Action { get; set; } = "Block";

    /// <summary>
    /// Evaluator-specific configuration.
    /// </summary>
    public JsonElement? Config { get; set; }

    /// <summary>
    /// Controls evaluation order within the guardrail.
    /// </summary>
    public int SortOrder { get; set; }
}
