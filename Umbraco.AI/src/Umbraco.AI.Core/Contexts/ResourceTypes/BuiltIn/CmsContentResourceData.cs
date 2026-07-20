namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resolved data for the live CMS-content resource type — a permission-checked snapshot of a content
/// node's current values, taken at resolve time.
/// </summary>
public sealed class CmsContentResourceData
{
    /// <summary>The content node's name.</summary>
    public string? Name { get; set; }

    /// <summary>Rendered plain-text of the node's property values.</summary>
    public string? Content { get; set; }
}
