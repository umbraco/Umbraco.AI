namespace Umbraco.AI.Web.Api.Management.SemanticSearch.Models;

/// <summary>
/// Response model for semantic search index status.
/// </summary>
public class SemanticSearchStatusResponseModel
{
    /// <summary>
    /// Gets or sets the total number of indexed documents.
    /// </summary>
    public int TotalIndexed { get; set; }

    /// <summary>
    /// Gets or sets the embedding profile ID used for indexing.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the model identifier used for indexing.
    /// </summary>
    public string? ModelId { get; set; }
}
