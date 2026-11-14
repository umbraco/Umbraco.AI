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

    public static AbilityResult Denied(string reason, string path = "")
        => AbilityResult.Fail(AbilityOperationStatus.AccessDenied, reason, [new Problem(path, reason)]);
}
