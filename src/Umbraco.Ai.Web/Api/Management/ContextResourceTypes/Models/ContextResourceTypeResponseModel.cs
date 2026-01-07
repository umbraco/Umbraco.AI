namespace Umbraco.Ai.Web.Api.Management.ContextResourceTypes.Models;

/// <summary>
/// Response model for a context resource type.
/// </summary>
public class ContextResourceTypeResponseModel
{
    /// <summary>
    /// The unique identifier of the resource type (e.g., "text", "brand-voice").
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The display name for the resource type.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The description of the resource type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The icon alias for the resource type.
    /// </summary>
    public string? Icon { get; set; }
}
