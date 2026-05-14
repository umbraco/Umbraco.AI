using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Builds expose entries for block envelopes.
/// </summary>
/// <remarks>
/// Each expose entry asserts that a block's content is "exposed" (visible) for a given
/// culture/segment combination. Invariant content uses a single entry with both fields
/// <c>null</c>; variant content uses one entry per active variant.
/// </remarks>
internal static class ExposeBuilder
{
    /// <summary>
    /// Returns a list of expose entries for the given content key and variants.
    /// </summary>
    /// <param name="contentKey">The block's content key.</param>
    /// <param name="variants">
    /// The variants to expose for. When the list is empty or only contains invariant entries, a
    /// single invariant expose entry is returned.
    /// </param>
    public static IEnumerable<JsonObject> Build(Guid contentKey, IReadOnlyList<AIVariantId> variants)
    {
        if (variants.Count == 0)
        {
            yield return new JsonObject
            {
                ["contentKey"] = contentKey,
                ["culture"] = null,
                ["segment"] = null,
            };
            yield break;
        }

        foreach (var variant in variants)
        {
            yield return new JsonObject
            {
                ["contentKey"] = contentKey,
                ["culture"] = variant.Culture,
                ["segment"] = variant.Segment,
            };
        }
    }
}
