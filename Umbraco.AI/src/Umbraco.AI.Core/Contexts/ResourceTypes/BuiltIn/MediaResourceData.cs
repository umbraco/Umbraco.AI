namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resolved data for the live Media resource type — a permission-checked snapshot of a published media
/// item, taken at resolve time and serialized with the same formatter the content tools use (name,
/// file URL, and formatted property values).
/// </summary>
public sealed class MediaResourceData
{
    /// <summary>The media item's name (used as the section heading).</summary>
    public string? Name { get; set; }

    /// <summary>The item serialized as JSON (name, url, and formatted property values).</summary>
    public string? Json { get; set; }
}
