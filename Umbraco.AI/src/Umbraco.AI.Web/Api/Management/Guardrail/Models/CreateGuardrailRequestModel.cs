using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Models;

/// <summary>
/// Request model for creating a new guardrail.
/// </summary>
public class CreateGuardrailRequestModel
{
    /// <summary>
    /// The alias of the guardrail (immutable after creation).
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
    /// The rules to create with this guardrail.
    /// </summary>
    public IReadOnlyList<GuardrailRuleModel> Rules { get; set; } = [];
}
