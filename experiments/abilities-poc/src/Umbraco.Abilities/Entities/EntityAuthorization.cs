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
    /// have. Items the entity already had are left alone. Each item's type is looked up in its own field's group, since
    /// the same alias can be a different type in another group.
    /// </summary>
    public static IReadOnlyList<string> NewItemTypesNotAvailable(EntityDocument? before, EntityDocument after, ElementShape shape, ItemTypeRegistry itemTypes, Guid userKey)
    {
        var existing = before is null ? [] : Items(before.Root, shape, itemTypes).Select(i => i.Item.Key).ToHashSet();
        return Items(after.Root, shape, itemTypes)
            .Where(i => !existing.Contains(i.Item.Key) && i.Type is { } type && !type.IsAvailableTo(userKey))
            .Select(i => i.Item.Type)
            .Distinct()
            .ToList();
    }

    // Every item in an element, with its item type, including items in items, settings and placement.
    private static IEnumerable<(EntityItem Item, ItemTypeDefinition? Type)> Items(EntityElement element, ElementShape shape, ItemTypeRegistry itemTypes)
    {
        foreach (var field in shape.ItemFields)
        {
            foreach (var item in element.ItemValues(field.Alias).SelectMany(v => v.Items))
            {
                var type = itemTypes.Find(field.ItemTypeGroup!, item.Type);
                yield return (item, type);
                IEnumerable<(EntityItem, ItemTypeDefinition?)> nested = type is null ? [] : Items(item, type.Shape, itemTypes);
                if (type?.SettingsShape is { } settingsShape && item.Settings is { } settings)
                {
                    nested = nested.Concat(Items(settings, settingsShape, itemTypes));
                }

                if (field.ItemLayout is { } layoutShape && item.Layout is { } layout)
                {
                    nested = nested.Concat(Items(layout, layoutShape, itemTypes));
                }

                foreach (var inner in nested)
                {
                    yield return inner;
                }
            }
        }
    }

    public static AbilityResult Denied(string reason, string path = "")
        => AbilityResult.Fail(AbilityOperationStatus.AccessDenied, reason, [new Problem(path, reason)]);
}
