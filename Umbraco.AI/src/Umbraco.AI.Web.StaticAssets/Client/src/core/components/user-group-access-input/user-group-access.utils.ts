const sameKey = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();

const includesKey = (keys: readonly string[], key: string) => keys.some((k) => sameKey(k, key));

/**
 * Whether the user can use an item limited to these groups: an empty list means everyone can,
 * otherwise the user must share a group with it. Admins can always use it.
 */
export function canUseRestrictedItem(
    allowedUserGroupIds: readonly string[],
    userGroupIds: readonly string[],
    isAdmin: boolean,
): boolean {
    if (isAdmin || allowedUserGroupIds.length === 0) return true;
    return allowedUserGroupIds.some((allowed) => includesKey(userGroupIds, allowed));
}

/**
 * Whether the user may remove a group from an item's access list. Admins may remove any group.
 * Anyone else may only remove a group they belong to, and never the last one, so they can't lock
 * themselves out of the item.
 */
export function canRemoveUserGroup(
    userGroupId: string,
    allowedUserGroupIds: readonly string[],
    userGroupIds: readonly string[],
    isAdmin: boolean,
): boolean {
    if (isAdmin) return true;
    return includesKey(userGroupIds, userGroupId) && allowedUserGroupIds.length > 1;
}

/** Adds the groups that aren't already in the list, keeping the existing order. */
export function addUserGroupIds(current: readonly string[], added: readonly string[]): string[] {
    const result = [...current];
    for (const id of added) {
        if (!includesKey(result, id)) result.push(id);
    }
    return result;
}

export function removeUserGroupId(current: readonly string[], removed: string): string[] {
    return current.filter((id) => !sameKey(id, removed));
}
