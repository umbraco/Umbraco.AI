using Umbraco.Cms.Core.Security;

using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Core.Security;

/// <inheritdoc />
internal sealed class AIPermissionService(IBackOfficeSecurityAccessor backOfficeSecurityAccessor) : IAIPermissionService
{
    /// <inheritdoc />
    public bool HasPermission(string verb)
    {
        var groups = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Groups;
        return groups is not null && groups.Any(group => group.Permissions.Contains(verb));
    }

    /// <inheritdoc />
    public bool CanUse(IReadOnlyCollection<Guid>? allowedUserGroupIds, IReadOnlyCollection<Guid> userGroupKeys)
        => allowedUserGroupIds is null
           || allowedUserGroupIds.Count == 0
           || allowedUserGroupIds.Any(userGroupKeys.Contains);

    /// <inheritdoc />
    public bool CanCurrentUserUse(IReadOnlyCollection<Guid>? allowedUserGroupIds)
        => CanUse(allowedUserGroupIds, GetCurrentUserGroups().GroupKeys);

    /// <inheritdoc />
    public IReadOnlyCollection<Guid>? GetListFilter(string manageVerb, bool includeRestricted)
        => HasManagedAccess(manageVerb, includeRestricted) ? null : GetCurrentUserGroups().GroupKeys;

    /// <inheritdoc />
    public bool IsVisible(IReadOnlyCollection<Guid>? allowedUserGroupIds, string manageVerb, bool includeRestricted)
        => HasManagedAccess(manageVerb, includeRestricted) || CanCurrentUserUse(allowedUserGroupIds);

    private bool HasManagedAccess(string manageVerb, bool includeRestricted)
        => includeRestricted && HasPermission(manageVerb);

    /// <inheritdoc />
    public AICurrentUserGroups GetCurrentUserGroups()
    {
        var groups = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Groups;
        if (groups is null)
        {
            return AICurrentUserGroups.None;
        }

        var keys = groups.Select(group => group.Key).ToArray();
        return new AICurrentUserGroups(keys, keys.Contains(CmsConstants.Security.AdminGroupKey));
    }
}
