using System.Text.Json.Nodes;

namespace Umbraco.Abilities.Entities;

public enum EntityPermission
{
    Read,
    Create,
    Update,
}

/// <summary>
/// Whether a user may read, create or change an entity. <paramref name="Document"/> is the entity as it would be after
/// the change (so a create can check its parent), or null for a read by key. <paramref name="Variants"/> are the variants
/// the change touches, for products that limit users to some of them (the CMS's languages).
/// </summary>
public sealed record EntityAccessRequest(Guid UserKey, EntityPermission Permission, Guid Key, EntityDocument? Document, IReadOnlySet<VariantId> Variants);

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

    /// <summary>The variants whose values differ between two versions of an entity.</summary>
    public static IReadOnlySet<VariantId> VariantsChanged(EntityDocument? before, EntityDocument after, ElementShape shape, ItemTypeRegistry itemTypes)
        => EntityDiff.Compare(before?.Root ?? [], after.Root, shape, itemTypes)
            .Select(c => c.Variant)
            .Where(v => !v.IsInvariant)
            .ToHashSet();

    /// <summary>
    /// Item types the change adds that the user may not use, such as an Automate action that needs a section they don't
    /// have. Items the entity already had are left alone. Each item's type is looked up in its own field's group, since
    /// the same alias can be a different type in another group.
    /// </summary>
    public static IReadOnlyList<string> NewItemTypesNotAvailable(EntityDocument? before, EntityDocument after, ElementShape shape, ItemTypeRegistry itemTypes, Guid userKey)
    {
        var existing = before is null ? [] : Items(before.Root, shape, itemTypes).Select(i => i.Key).ToHashSet();
        return Items(after.Root, shape, itemTypes)
            .Where(i => !existing.Contains(i.Key) && i.Type is { } type && !type.IsAvailableTo(userKey))
            .Select(i => i.Type!.Alias)
            .Distinct()
            .ToList();
    }

    // Every item in the entity, at any depth, with its item type.
    private static IEnumerable<(Guid Key, ItemTypeDefinition? Type)> Items(JsonObject root, ElementShape shape, ItemTypeRegistry itemTypes)
        => EntityValues.Walk(root, shape, itemTypes)
            .Where(v => v.Field.IsItems)
            .SelectMany(v => (v.Value as JsonArray ?? []).OfType<JsonObject>()
                .Select(item => (EntityValues.KeyOf(item), itemTypes.Find(v.Field.ItemTypeGroup!, EntityValues.TypeOf(item)))));

    public static AbilityResult Denied(string reason, string path = "")
        => AbilityResult.Fail(AbilityOperationStatus.AccessDenied, reason, [new Problem(path, reason)]);
}
