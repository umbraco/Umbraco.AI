using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Models;

/// <summary>
/// Full response model for a guardrail (includes rules).
/// </summary>
public class GuardrailResponseModel
{
    /// <summary>
    /// The unique identifier of the guardrail.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The alias of the guardrail.
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the guardrail.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the guardrail was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Date and time when the guardrail was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The rules belonging to this guardrail.
    /// </summary>
    public IReadOnlyList<GuardrailRuleModel> Rules { get; set; } = [];

    /// <summary>
    /// The current version number of the entity.
    /// </summary>
    public int Version { get; set; }
}
