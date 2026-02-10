/**
 * User group-specific tool permission overrides for an agent.
 */
export interface UaiUserGroupPermissionsModel {
	/**
	 * Tool IDs explicitly allowed for this user group (additive).
	 */
	allowedToolIds: string[];

	/**
	 * Tool scope IDs allowed for this user group (additive).
	 * Tools matching these scopes will be included automatically.
	 */
	allowedToolScopeIds: string[];

	/**
	 * Tool IDs explicitly denied for this user group (subtractive).
	 * Takes precedence over agent defaults and allowed overrides.
	 */
	deniedToolIds: string[];

	/**
	 * Tool scope IDs denied for this user group (subtractive).
	 * Tools matching these scopes will be excluded.
	 * Takes precedence over agent defaults and allowed overrides.
	 */
	deniedToolScopeIds: string[];
}

/**
 * Map of user group IDs to their permission overrides.
 * Dictionary key is the user group ID (GUID as string).
 */
export interface UaiUserGroupPermissionsMap {
	[userGroupId: string]: UaiUserGroupPermissionsModel;
}
