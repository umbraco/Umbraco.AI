using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Common.Models;

/// <summary>
/// A reference to a specific AI model provided by an AI provider.
/// </summary>
public class ModelRefModel
{
    /// <summary>
    /// The ID of the AI provider.
    /// </summary>
    [Required]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the AI model.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;
}
