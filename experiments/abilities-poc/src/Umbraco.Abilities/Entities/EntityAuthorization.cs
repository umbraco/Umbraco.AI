namespace Umbraco.Abilities.Entities;

public enum EntityPermission
{
    Read,
    Create,
    Update,
}

/// <summary>
/// Whether a user may read, create or change an entity. <paramref name="Document"/> is the entity as it would be after
/// the change (so a create can check its parent), or null for a read by key. <paramref name="Cultures"/> are the cultures
/// the change touches, for products that limit users to some languages.
/// </summary>
public sealed record EntityAccessRequest(Guid UserKey, EntityPermission Permission, Guid Key, EntityDocument? Document, IReadOnlySet<string> Cultures);

/// <summary>
/// A definition whose product has its own permission model: CMS user groups, document permissions and language access, or
/// Commerce's store access. Abilities never build their own: they ask the product. A definition without this allows everyone.
/// </summary>
public interface IAuthorizingEntityDefinition : IEntityDefinition
{
    /// <summary>Null when allowed; otherwise why not, in words the user (or an agent acting for them) can be told.</summary>
    Task<string?> AuthorizeAsync(EntityAccessRequest request, CancellationToken cancellationToken = default);
}

public static class EntityAccess
{
    public static async Task<string?> CheckAsync(IEntityDefinition definition, EntityAccessRequest request, CancellationToken cancellationToken = default)
        => definition is IAuthorizingEntityDefinition authorizing ? await authorizing.AuthorizeAsync(request, cancellationToken) : null;

    /// <summary>The cultures whose values differ between two versions of an entity.</summary>
    public static IReadOnlySet<string> CulturesChanged(EntityDocument? before, EntityDocument after)
        => EntityDiff.Compare((before ?? new EntityDocument { EntityType = after.EntityType, Key = after.Key }).Root, after.Root)
            .Select(c => c.Variant.Culture)
            .OfType<string>()
            .ToHashSet();

    /// <summary>
    /// Item types the change adds that the user may not use, such as an Automate action that needs a section they don't
    /// have. Items the entity already had are left alone.
    /// </summary>
    public static IReadOnlyList<string> NewItemTypesNotAvailable(EntityDocument? before, EntityDocument after, ItemTypeRegistry itemTypes, Guid userKey)
    {
        var existing = before is null ? [] : Items(before.Root).Select(i => i.Key).ToHashSet();
        return Items(after.Root)
            .Where(i => !existing.Contains(i.Key))
            .Select(i => i.Type)
            .Distinct()
            .Where(type => itemTypes.Find(type) is { } itemType && !itemType.IsAvailableTo(userKey))
            .ToList();
    }

    private static IEnumerable<EntityItem> Items(EntityElement element)
    {
        var parts = new ElementValues[] { element }.Concat(element.Variants.Values);
        foreach (var item in parts.SelectMany(p => p.Items.Values).SelectMany(items => items))
        {
            yield return item;
            foreach (var nested in Items(item))
            {
                yield return nested;
            }
        }
    }

    public static AbilityResult Denied(string reason, string path = "")
        => AbilityResult.Fail(AbilityOperationStatus.AccessDenied, reason, [new Problem(path, reason)]);
}
