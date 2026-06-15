using System.Text.Json.Serialization;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// A single segment of a property value path. Either a property alias (string) or a block key selector
/// (<c>{ "blockKey": "..." }</c> at the JSON level).
/// </summary>
/// <remarks>
/// Paths alternate between property aliases and block selectors, e.g.
/// <c>["contentBlocks", { "blockKey": "..." }, "innerBlocks"]</c>. The dispatcher walks the path by
/// resolving each property alias against the active value's schema and descending into the named block
/// when a selector is encountered.
/// </remarks>
[JsonConverter(typeof(AIPropertyPathSegmentJsonConverter))]
public abstract record AIPropertyPathSegment
{
    private AIPropertyPathSegment() { }

    /// <summary>
    /// Creates a property alias segment.
    /// </summary>
    /// <param name="alias">The property alias.</param>
    /// <returns>The new segment.</returns>
    public static AIPropertyPathSegment ForProperty(string alias) => new PropertyAliasSegment(alias);

    /// <summary>
    /// Creates a block key segment.
    /// </summary>
    /// <param name="key">The block key (the <c>contentKey</c> of an item in the parent value's <c>contentData</c>).</param>
    /// <returns>The new segment.</returns>
    public static AIPropertyPathSegment ForBlock(Guid key) => new BlockKeySegment(key);

    /// <summary>
    /// Identifies a property by its alias, scoped to the current frame in the path walk.
    /// </summary>
    /// <param name="Alias">The property alias.</param>
    public sealed record PropertyAliasSegment(string Alias) : AIPropertyPathSegment;

    /// <summary>
    /// Identifies a block (item) within a collection-shaped property value by its key.
    /// </summary>
    /// <param name="BlockKey">The block <c>contentKey</c>.</param>
    public sealed record BlockKeySegment(Guid BlockKey) : AIPropertyPathSegment;
}
