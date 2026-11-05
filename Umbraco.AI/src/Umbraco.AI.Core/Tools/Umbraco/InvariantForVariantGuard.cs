using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Refuses writes to shared (culture-invariant) properties on culture-variant content when the acting
/// user's groups lack the CMS 19 invariant-for-variant permission. The CMS does not fail such an edit —
/// <c>ContentEditingServiceBase.EnsureOnlyAllowedFieldsAreUpdated</c> reverts it and the save still
/// succeeds — so without this check a tool would report success for a change that was never kept.
/// </summary>
internal static class InvariantForVariantGuard
{
    /// <summary>
    /// Returns a user-facing message when the write must be refused, otherwise <c>null</c>.
    /// </summary>
    /// <param name="authResult">The authorization result for the acting user.</param>
    /// <param name="contentVariesByCulture">Whether the content type varies by culture.</param>
    /// <param name="propertyTypes">The content's property types, used to find which aliases are shared.</param>
    /// <param name="aliases">The property aliases being written.</param>
    public static string? GetDenialMessage(
        UmbracoWriteAuthorizationResult authResult,
        bool contentVariesByCulture,
        IEnumerable<IPropertyType> propertyTypes,
        IEnumerable<string> aliases)
    {
        if (!contentVariesByCulture || authResult.HasInvariantForVariantAccess)
        {
            return null;
        }

        var types = propertyTypes.ToList();
        var sharedAliases = aliases
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(alias => types.FirstOrDefault(p => string.Equals(p.Alias, alias, StringComparison.OrdinalIgnoreCase)) is { } type
                && !type.VariesByCulture())
            .ToArray();

        return sharedAliases.Length == 0
            ? null
            : "You do not have permission to edit shared (culture-invariant) properties on culture-variant content: " +
              $"{string.Join(", ", sharedAliases.Select(a => $"'{a}'"))}. " +
              "Your user group needs the invariant-for-variant permission, which an administrator can grant. " +
              "Nothing was changed. You can retry without those properties.";
    }

    /// <summary>
    /// For a shared block/rich-text property the CMS merges instead of reverting: the root's layout and
    /// shared values are reverted, a culture-variant property inside a block is kept whole (everything
    /// beneath it included), and an invariant one is reverted whole. Returns a message when the edit would
    /// be reverted, otherwise <c>null</c>.
    /// </summary>
    /// <param name="rootAlias">The shared root property being edited.</param>
    /// <param name="nestedVariesByCulture">
    /// Whether the first property below the root varies by culture; <c>null</c> when the edit targets the
    /// root itself (root value or root layout), which is always reverted.
    /// </param>
    public static string? GetMergeDenialMessage(string rootAlias, bool? nestedVariesByCulture)
    {
        if (nestedVariesByCulture is true)
        {
            return null;
        }

        return $"You do not have permission to change the shared (culture-invariant) part of '{rootAlias}' on culture-variant content: " +
               "adding, removing or moving its blocks, replacing the whole value, and editing shared properties inside its blocks are not allowed. " +
               "Your user group needs the invariant-for-variant permission, which an administrator can grant. " +
               "You can still edit the culture-variant properties of its blocks (and anything inside them). Nothing was changed.";
    }
}
