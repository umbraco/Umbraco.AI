using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Models;

/// <summary>
/// Request model for updating an existing guardrail.
/// </summary>
public class UpdateGuardrailRequestModel
{
    /// <summary>
    /// The alias of the guardrail (can be updated).
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the guardrail.
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The rules for this guardrail (replaces existing rules).
    /// </summary>
    public IReadOnlyList<GuardrailRuleModel> Rules { get; set; } = [];
}
