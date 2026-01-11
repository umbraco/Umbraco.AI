/**
 * Entity Adapter Types
 *
 * Minimal interfaces for the entity adapter system that enables
 * AI tools to interact with Umbraco entities being edited.
 */

/**
 * Represents the identity of an entity being edited.
 * Supports hierarchical relationships via recursive parentContext.
 */
export interface UaiEntityContext {
	entityType: string;
	unique: string | null; // null for "create" scenarios
	parentContext?: UaiEntityContext;
}

/**
 * Serialized representation of an entity for LLM context.
 */
export interface UaiSerializedEntity {
	entityType: string;
	unique: string;
	name: string;
	contentType?: string;
	/** Parent unique when creating a new entity. Undefined for existing entities. */
	parentUnique?: string | null;
	properties: UaiSerializedProperty[];
}

/**
 * Serialized property for LLM context.
 */
export interface UaiSerializedProperty {
	alias: string;
	label: string;
	editorAlias: string;
	value: unknown;
}

/**
 * Request to change a property value.
 * Changes are staged in the workspace - user must save to persist.
 */
export interface UaiPropertyChange {
	/** Property alias */
	alias: string;
	/** New value to set */
	value: unknown;
	/** Culture for variant content (undefined = invariant) */
	culture?: string;
	/** Segment for segmented content (undefined = no segment) */
	segment?: string;
}

/**
 * Result of a property change operation.
 */
export interface UaiPropertyChangeResult {
	/** Whether the change was applied successfully */
	success: boolean;
	/** Human-readable error message if failed */
	error?: string;
}

/**
 * Entity adapter API interface.
 * Adapters are responsible for:
 * - Detecting if they can handle a workspace context
 * - Extracting entity identity from workspace context
 * - Serializing entity data for LLM consumption
 * - Applying property changes (optional)
 */
export interface UaiEntityAdapterApi {
	readonly entityType: string;

	/**
	 * Check if this adapter can handle the given workspace context.
	 */
	canHandle(workspaceContext: unknown): boolean;

	/**
	 * Extract entity identity from workspace context.
	 */
	extractEntityContext(workspaceContext: unknown): UaiEntityContext;

	/**
	 * Get the current display name for the entity.
	 * Used for initial name population.
	 */
	getName(workspaceContext: unknown): string;

	/**
	 * Get an observable for the entity name for reactive updates.
	 * Returns undefined if the adapter doesn't support reactive names.
	 */
	getNameObservable?(workspaceContext: unknown): import("@umbraco-cms/backoffice/external/rxjs").Observable<string | undefined> | undefined;

	/**
	 * Get the icon for the entity.
	 * Used for initial icon population.
	 */
	getIcon?(workspaceContext: unknown): string | undefined;

	/**
	 * Get an observable for the entity icon for reactive updates.
	 * Returns undefined if the adapter doesn't support reactive icons.
	 */
	getIconObservable?(workspaceContext: unknown): import("@umbraco-cms/backoffice/external/rxjs").Observable<string | undefined> | undefined;

	/**
	 * Serialize the entity for LLM context.
	 */
	serializeForLlm(workspaceContext: unknown): Promise<UaiSerializedEntity>;

	/**
	 * Apply a property change to the workspace (staged, not persisted).
	 * Optional - some entity types may be read-only.
	 * @param workspaceContext The workspace context to modify
	 * @param change The property change to apply
	 * @returns Result indicating success or failure with error message
	 */
	applyPropertyChange?(
		workspaceContext: unknown,
		change: UaiPropertyChange,
	): Promise<UaiPropertyChangeResult>;
}

/**
 * Detected entity with its adapter and workspace context.
 * Used internally by the entity adapter context.
 */
export interface UaiDetectedEntity {
	/** Unique key: entityType:unique */
	key: string;
	/** Display name for UI */
	name: string;
	/** Icon name for UI */
	icon?: string;
	/** Entity identity */
	entityContext: UaiEntityContext;
	/** The adapter that handles this entity */
	adapter: UaiEntityAdapterApi;
	/** Live workspace context instance */
	workspaceContext: object;
}
