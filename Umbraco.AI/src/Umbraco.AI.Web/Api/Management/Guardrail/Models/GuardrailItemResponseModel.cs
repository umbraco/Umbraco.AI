using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Models;

/// <summary>
/// Lightweight response model for a guardrail item (used in lists).
/// </summary>
public class GuardrailItemResponseModel
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
    /// Number of rules in the guardrail.
    /// </summary>
    public int RuleCount { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the guardrail was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the guardrail was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }
}
