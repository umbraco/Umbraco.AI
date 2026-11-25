namespace Umbraco.AI.Core.Security;

/// <summary>
/// Decides AI rights from the current backoffice user's groups, and who may use an agent or prompt.
/// </summary>
internal interface IAIPermissionService
{
    /// <summary>
    /// Whether any group of the current backoffice user holds the verb. There is no admin bypass.
    /// Returns <c>false</c> when there is no current backoffice user.
    /// </summary>
    /// <param name="verb">One of the <see cref="AIPermissions"/> constants.</param>
    bool HasPermission(string verb);

    /// <summary>
    /// Whether a user may use an agent or prompt limited to the given groups. A null or empty list
    /// means every user may. Otherwise the user needs at least one of the groups.
    /// </summary>
    /// <param name="allowedUserGroupIds">The saved limit, or null or empty for no limit.</param>
    /// <param name="userGroupKeys">The keys of the user's groups.</param>
    bool CanUse(IReadOnlyCollection<Guid>? allowedUserGroupIds, IReadOnlyCollection<Guid> userGroupKeys);

    /// <summary>
    /// Whether the current backoffice user may use an item limited to the given groups, going by their own groups.
    /// Managing the item's type does not lift the limit.
    /// </summary>
    /// <param name="allowedUserGroupIds">The saved limit, or null or empty for no limit.</param>
    bool CanCurrentUserUse(IReadOnlyCollection<Guid>? allowedUserGroupIds);

    /// <summary>
    /// Gets the group keys to filter a list of limited items by, or <c>null</c> when no limit applies. No limit applies
    /// when the caller asked for managed access (<paramref name="includeRestricted"/>) and the current user holds the
    /// manage verb. Without a current backoffice user the verb is never held, so a headless caller never gets it.
    /// </summary>
    /// <param name="manageVerb">The add-on's manage right, e.g. its <c>Manage</c> permission verb.</param>
    /// <param name="includeRestricted">Whether the caller asked for the items the user could only manage.</param>
    IReadOnlyCollection<Guid>? GetListFilter(string manageVerb, bool includeRestricted);

    /// <summary>
    /// Whether one limited item is visible to the current backoffice user: managed access
    /// (see <see cref="GetListFilter"/>) or membership of one of the item's groups.
    /// </summary>
    /// <param name="allowedUserGroupIds">The saved limit, or null or empty for no limit.</param>
    /// <param name="manageVerb">The add-on's manage right.</param>
    /// <param name="includeRestricted">Whether the caller asked for the items the user could only manage.</param>
    bool IsVisible(IReadOnlyCollection<Guid>? allowedUserGroupIds, string manageVerb, bool includeRestricted);

    /// <summary>
    /// Gets the current backoffice user's group keys and whether they are in the admin group.
    /// Empty when there is no current backoffice user.
    /// </summary>
    AICurrentUserGroups GetCurrentUserGroups();
}

/// <summary>
/// The current user's group keys and whether they are in the admin group.
/// </summary>
/// <param name="GroupKeys">The keys of the user's groups.</param>
/// <param name="IsAdmin">Whether the user is in the admin group.</param>
internal sealed record AICurrentUserGroups(IReadOnlyCollection<Guid> GroupKeys, bool IsAdmin)
{
    /// <summary>No groups, not an admin.</summary>
    public static AICurrentUserGroups None { get; } = new(Array.Empty<Guid>(), false);
}
