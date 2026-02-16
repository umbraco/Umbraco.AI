/**
 * Entity Adapter Types
 *
 * Minimal interfaces for the entity adapter system that enables
 * AI tools to interact with Umbraco entities being edited.
 */

import { UmbApi } from "@umbraco-cms/backoffice/extension-api";

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
 * Adapters decide the structure of the data field based on entity type.
 */
export interface UaiSerializedEntity {
    entityType: string;
    unique: string;
    name: string;
    /** Parent unique when creating a new entity. Undefined for existing entities. */
    parentUnique?: string | null;
    /**
     * Free-form entity data as JSON object.
     * Adapters decide the structure based on entity type.
     *
     * For CMS entities, typically contains:
     * ```typescript
     * {
     *   contentType: "blogPost",
     *   properties: [
     *     { alias: "title", label: "Title", editorAlias: "Umbraco.TextBox", value: "Hello" }
     *   ]
     * }
     * ```
     *
     * For third-party entities, can be any domain-appropriate structure:
     * ```typescript
     * {
     *   sku: "12345",
     *   price: { amount: 29.99, currency: "USD" },
     *   variants: [{ color: "red", size: "large" }]
     * }
     * ```
     */
    data: Record<string, unknown>;
}

/**
 * Serialized property for LLM context.
 * @deprecated Entity data is now stored in UaiSerializedEntity.data as free-form JSON.
 * For CMS entities, properties are nested inside the data field.
 * This interface is kept for reference only.
 */
export interface UaiSerializedProperty {
    alias: string;
    label: string;
    editorAlias: string;
    value: unknown;
}

/**
 * Request to change a value at a JSON path in the entity data.
 * Changes are staged in the workspace - user must save to persist.
 */
export interface UaiValueChange {
    /** JSON path to the value (e.g., "title", "price.amount", "inventory.quantity") */
    path: string;
    /** New value to set */
    value: unknown;
    /** Culture for variant content (undefined = invariant) */
    culture?: string;
    /** Segment for segmented content (undefined = no segment) */
    segment?: string;
}

/**
 * Result of a value change operation.
 */
export interface UaiValueChangeResult {
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
export interface UaiEntityAdapterApi extends UmbApi {
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
    getNameObservable?(
        workspaceContext: unknown,
    ): import("@umbraco-cms/backoffice/external/rxjs").Observable<string | undefined> | undefined;

    /**
     * Get the icon for the entity.
     * Used for initial icon population.
     */
    getIcon?(workspaceContext: unknown): string | undefined;

    /**
     * Get an observable for the entity icon for reactive updates.
     * Returns undefined if the adapter doesn't support reactive icons.
     */
    getIconObservable?(
        workspaceContext: unknown,
    ): import("@umbraco-cms/backoffice/external/rxjs").Observable<string | undefined> | undefined;

    /**
     * Serialize the entity for LLM context.
     */
    serializeForLlm(workspaceContext: unknown): Promise<UaiSerializedEntity>;

    /**
     * Apply a value change to the workspace (staged, not persisted).
     * Optional - some entity types may be read-only.
     * @param workspaceContext The workspace context to modify
     * @param change The value change to apply
     * @returns Result indicating success or failure with error message
     */
    applyValueChange?(workspaceContext: unknown, change: UaiValueChange): Promise<UaiValueChangeResult>;
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
