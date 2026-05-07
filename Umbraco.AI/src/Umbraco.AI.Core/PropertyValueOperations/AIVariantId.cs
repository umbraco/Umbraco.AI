namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Identifies a culture/segment combination for variant-aware property values.
/// </summary>
/// <param name="Culture">The culture code (e.g. "en-US"), or <c>null</c> for invariant content.</param>
/// <param name="Segment">The segment alias, or <c>null</c> for non-segmented content.</param>
public sealed record AIVariantId(string? Culture, string? Segment)
{
    /// <summary>
    /// Gets a variant identifier representing invariant, non-segmented content.
    /// </summary>
    public static AIVariantId Invariant { get; } = new(null, null);

    /// <summary>
    /// Gets a value indicating whether this identifier represents invariant, non-segmented content.
    /// </summary>
    public bool IsInvariant => Culture is null && Segment is null;
}
