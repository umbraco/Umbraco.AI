using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Umbraco.AI.Core.PropertyValueOperations;

namespace Umbraco.AI.Web.Api.Management.PropertyValueOperation.Models;

/// <summary>
/// Request payload for the property value operation endpoint.
/// </summary>
public sealed class PropertyValueOperationRequestModel
{
    /// <summary>
    /// The path identifying the leaf to operate on. Property aliases at even indices, block-key
    /// selectors at odd indices.
    /// </summary>
    [Required]
    public IList<AIPropertyPathSegment> Path { get; set; } = new List<AIPropertyPathSegment>();

    /// <summary>
    /// The kind of operation to perform.
    /// </summary>
    [Required]
    public AIPropertyOperation Operation { get; set; }

    /// <summary>
    /// Operation-specific arguments. Shape varies per operation; see <c>IAIPropertyValueDispatcher</c>
    /// documentation.
    /// </summary>
    public JsonNode? Args { get; set; }

    /// <summary>
    /// The current value of the root property the path begins in. The endpoint never reads the
    /// value from the database; it operates purely on the value supplied here. Frontend tools send
    /// the workspace's staged value (preserving unsaved user edits).
    /// </summary>
    public JsonNode? RootValue { get; set; }

    /// <summary>
    /// Document-level metadata required by the dispatcher.
    /// </summary>
    [Required]
    public AIDocumentMetadata DocumentMetadata { get; set; } = new(
        ContentTypeKey: Guid.Empty,
        Variants: new List<AIVariantId> { new(null, null) },
        IsVariant: false,
        IsSegmented: false);
}
