using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Web.Api.Management.Common.Models;

namespace Umbraco.Ai.Web.Api.Management.Provider.Models;

/// <summary>
/// Response model for a model descriptor.
/// </summary>
public class ModelDescriptorResponseModel
{
    /// <summary>
    /// The reference to the AI model.
    /// </summary>
    [Required]
    public ModelRefModel? Model { get; set; }

    /// <summary>
    /// The display name of the AI model.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata associated with the AI model.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
}
