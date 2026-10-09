using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Management.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Connection.Models;

/// <summary>
/// Response model for the facts about one model.
/// </summary>
public class ModelFactsItemResponseModel
{
    /// <summary>
    /// The model the facts describe.
    /// </summary>
    [Required]
    public ModelRefModel Model { get; set; } = new();

    /// <summary>
    /// The model's facts, already in display order.
    /// </summary>
    [Required]
    public IEnumerable<ModelFactResponseModel> Facts { get; set; } = [];
}
