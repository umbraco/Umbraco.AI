namespace Umbraco.AI.Core.Security;

/// <summary>
/// The rule for changing a list of user groups on an agent or prompt, copied from the CMS's rule for
/// assigning user groups (<c>UserGroupAssignmentAuthorization</c> and <c>UserGroupService</c>, 18.3.2).
/// Admins change freely. Anyone else may only add or remove groups they belong to.
/// </summary>
internal static class AIUserGroupChangeRule
{
    /// <summary>
    /// Returns the groups a user may not add or remove: every group that differs between the two lists
    /// and is not one of the user's own. An empty result means the change is allowed.
    /// </summary>
    /// <param name="userGroupKeys">The keys of the groups the user belongs to.</param>
    /// <param name="existingGroupKeys">The groups before the change.</param>
    /// <param name="requestedGroupKeys">The groups after the change.</param>
    public static IReadOnlyList<Guid> GetUnauthorizedChanges(
        IEnumerable<Guid> userGroupKeys,
        IEnumerable<Guid> existingGroupKeys,
        IEnumerable<Guid> requestedGroupKeys)
    {
        var own = userGroupKeys.ToHashSet();
        var existing = existingGroupKeys.ToHashSet();
        var requested = requestedGroupKeys.ToHashSet();

        return requested.Except(existing)
            .Concat(existing.Except(requested))
            .Where(key => !own.Contains(key))
            .ToArray();
    }

    /// <summary>
    /// Refuses a change to a group list the user may not make. Does nothing when the list is unchanged.
    /// </summary>
    /// <param name="permissionService">Decides who can use an item and who the user is.</param>
    /// <param name="existingGroupKeys">The saved list, empty for a new item.</param>
    /// <param name="requestedGroupKeys">The list being saved.</param>
    /// <param name="subject">What is being saved, for the message, such as "agent 'support'".</param>
    /// <exception cref="AIPermissionDeniedException">Thrown when the change is not allowed.</exception>
    public static void EnsureCanChangeList(
        IAIPermissionService permissionService,
        IReadOnlyCollection<Guid> existingGroupKeys,
        IReadOnlyCollection<Guid> requestedGroupKeys,
        string subject)
    {
        if (existingGroupKeys.ToHashSet().SetEquals(requestedGroupKeys))
        {
            return;
        }

        var user = permissionService.GetCurrentUserGroups();
        if (user.IsAdmin)
        {
            return;
        }

        if (!permissionService.CanUse(existingGroupKeys, user.GroupKeys))
        {
            throw new AIPermissionDeniedException(
                $"You cannot change the user groups of {subject}, because you cannot use it.");
        }

        if (existingGroupKeys.Count > 0 && requestedGroupKeys.Count == 0)
        {
            throw new AIPermissionDeniedException(
                $"Only an administrator can remove every user group from {subject}, as that makes it available to everyone.");
        }

        if (GetUnauthorizedChanges(user.GroupKeys, existingGroupKeys, requestedGroupKeys).Count > 0)
        {
            throw new AIPermissionDeniedException(
                $"You can only add or remove user groups you belong to on {subject}.");
        }
    }
}
