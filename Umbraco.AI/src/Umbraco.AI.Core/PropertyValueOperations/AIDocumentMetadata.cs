namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Document-level context required by the property value dispatcher and handlers.
/// </summary>
/// <remarks>
/// The dispatcher endpoint never reads from the database; everything a handler needs to know
/// about the parent document (variants, content type, name) must be supplied in this metadata.
/// Frontend tools build this from the active workspace; future server-side tools build it from
/// the database read.
/// </remarks>
/// <param name="ContentTypeKey">The content type key of the root entity (document or media).</param>
/// <param name="Variants">
/// The variants the operation applies to (active culture/segment combinations). At least one entry
/// is required; invariant content uses a single entry with both fields <c>null</c>.
/// </param>
/// <param name="IsVariant">Whether the parent document is variant-aware (cultures).</param>
/// <param name="IsSegmented">Whether the parent document supports segmented content.</param>
/// <param name="Name">Optional display name of the parent entity, for diagnostic messages.</param>
public sealed record AIDocumentMetadata(
    Guid ContentTypeKey,
    IReadOnlyList<AIVariantId> Variants,
    bool IsVariant,
    bool IsSegmented,
    string? Name = null);
