using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Builds expose entries for block envelopes.
/// </summary>
/// <remarks>
/// Each expose entry asserts that a block's content is "exposed" (visible) for a given
/// culture. Invariant content uses a single entry with a <c>null</c> culture; variant content uses
/// one entry per active culture. Segments are not part of an expose entry (CMS 19).
/// </remarks>
internal static class ExposeBuilder
{
    /// <summary>
    /// Returns a list of expose entries for the given content key and variants.
    /// </summary>
    /// <param name="contentKey">The block's content key.</param>
    /// <param name="variants">
    /// The variants to expose for. Segments are ignored, so variants sharing a culture yield one
    /// entry. When the list is empty or only contains invariant entries, a single invariant expose
    /// entry is returned.
    /// </param>
    public static IEnumerable<JsonObject> Build(Guid contentKey, IReadOnlyList<AIVariantId> variants)
    {
        if (variants.Count == 0)
        {
            yield return new JsonObject
            {
                ["contentKey"] = contentKey,
                ["culture"] = null,
            };
            yield break;
        }

        foreach (var culture in variants.Select(v => v.Culture).Distinct())
        {
            yield return new JsonObject
            {
                ["contentKey"] = contentKey,
                ["culture"] = culture,
            };
        }
    }
}
