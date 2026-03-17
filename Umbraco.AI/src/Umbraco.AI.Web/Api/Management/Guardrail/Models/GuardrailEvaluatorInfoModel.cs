using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Management.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Models;

/// <summary>
/// Information about an available guardrail evaluator.
/// </summary>
public class GuardrailEvaluatorInfoModel
{
    /// <summary>
    /// The unique identifier for the evaluator.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the evaluator.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what this evaluator checks.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of evaluator (CodeBased or ModelBased).
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this evaluator supports the Redact action (implements IAIRedactableGuardrailEvaluator).
    /// </summary>
    public bool SupportsRedaction { get; set; }

    /// <summary>
    /// The schema for the evaluator configuration.
    /// Null if the evaluator does not require configuration.
    /// </summary>
    public EditableModelSchemaModel? ConfigSchema { get; set; }
}
