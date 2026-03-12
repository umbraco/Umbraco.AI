import type { UmbApi } from "@umbraco-cms/backoffice/extension-api";

/**
 * Entity data for display in test feature target pickers.
 * Minimal data needed to display available entities in the UI.
 */
export interface UaiTestFeatureEntityData {
	/** Unique entity identifier (can be GUID or alias) */
	id: string;
	/** Display name for the entity */
	name: string;
	/** Description of the entity (optional) */
	description?: string;
	/** Umbraco icon name (e.g., "icon-chat", "icon-robot") */
	icon: string;
}

/**
 * Repository API for accessing test feature entities.
 * Implemented by packages that provide testable entities (Prompt, Agent, etc.).
 *
 * Each test feature type can have zero or one repository implementation.
 */
export interface UaiTestFeatureEntityRepositoryApi extends UmbApi {
	/**
	 * Get all entities available for testing within this feature type.
	 * @returns Array of entity data for display in picker
	 */
	getEntities(): Promise<UaiTestFeatureEntityData[]>;

	/**
	 * Get a single entity by ID or alias.
	 * Used for resolving the entity when loading an existing test.
	 * @param idOrAlias - Entity ID (GUID) or alias
	 * @returns Entity data or undefined if not found
	 */
	getEntity(idOrAlias: string): Promise<UaiTestFeatureEntityData | undefined>;
}
