using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Connection.Models;

/// <summary>
/// Response model for the facts about a connection's models.
/// </summary>
public class ModelFactsResponseModel
{
    /// <summary>
    /// The models that have facts. Models without facts are omitted.
    /// </summary>
    [Required]
    public IEnumerable<ModelFactsItemResponseModel> Items { get; set; } = [];
}
