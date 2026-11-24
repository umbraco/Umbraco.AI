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
