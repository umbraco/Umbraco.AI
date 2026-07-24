namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resolved data for the live Content resource type — a permission-checked snapshot of a published
/// content node, taken at resolve time and serialized with the same formatter the <c>get_content</c>
/// tool uses (so the AI sees a node identically whether it fetches it or receives it as context).
/// </summary>
public sealed class ContentResourceData
{
    /// <summary>The content node's name (used as the section heading).</summary>
    public string? Name { get; set; }

    /// <summary>The node serialized as JSON (name, url, content type, and formatted property values).</summary>
    public string? Json { get; set; }
}
