/**
 * Whether a user group's row can be added to, changed or removed.
 * An unset list means every group is editable; an empty list means none is.
 */
export function isUserGroupEditable(userGroupId: string, editableUserGroupIds: readonly string[] | undefined): boolean {
    if (!editableUserGroupIds) return true;
    const id = userGroupId.toLowerCase();
    return editableUserGroupIds.some((editableId) => editableId.toLowerCase() === id);
}
