using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.ModelFacts;

namespace Umbraco.AI.Web.Api.Management.Connection.Models;

/// <summary>
/// Response model for a single fact about a model.
/// </summary>
public class ModelFactResponseModel
{
    /// <summary>
    /// The key that identifies this fact, for example <c>core.contextWindow</c>.
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The label. Plain text or a localization key starting with <c>#</c>.
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// An optional shorter label, used where space is tight. May be a <c>#</c> localization key.
    /// </summary>
    public string? ShortLabel { get; set; }

    /// <summary>
    /// The display text for the value, already formatted.
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// An optional numeric value used for sorting. <c>null</c> means the fact is not sortable.
    /// </summary>
    public double? SortValue { get; set; }

    /// <summary>
    /// Optional extra text, shown as a tooltip. May be a <c>#</c> localization key.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// How the fact should be emphasized.
    /// </summary>
    [Required]
    public AIModelFactTone Tone { get; set; }

    /// <summary>
    /// An optional "learn more" link. Always an absolute http or https URL when present.
    /// </summary>
    public string? Url { get; set; }
}
