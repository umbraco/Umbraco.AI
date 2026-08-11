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
     * Active culture the editor was on when the entity was serialized.
     * Null/undefined for invariant entities. Used by the server to pick the
     * matching property entry from `data.properties` on multi-variant content.
     */
    culture?: string | null;
    /** Active segment when the entity was serialized. */
    segment?: string | null;
    /**
     * Free-form entity data as JSON object.
     * Adapters decide the structure based on entity type.
     *
     * For CMS entities, typically contains:
     * ```typescript
     * {
     *   contentType: "blogPost",
     *   properties: [
     *     { alias: "title", label: "Title", editorAlias: "Umbraco.TextBox", value: "Hello", culture: "en-US", segment: null }
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
 * Used as the element type of `UaiSerializedEntity.data.properties` for CMS entities.
 *
 * `culture` and `segment` describe which variant this property value belongs to:
 * `null` means invariant (no culture/segment dimension on the property).
 */
export interface UaiSerializedProperty {
    alias: string;
    label: string;
    editorAlias: string;
    value: unknown;
    culture: string | null;
    segment: string | null;
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
 * Result of a save (or save-and-publish) operation triggered by an AI tool. Adapters convert
 * exceptions thrown by the underlying workspace into a structured payload so the LLM can read the
 * error message rather than treat the call as silently successful.
 */
export interface UaiPersistResult {
    /** Whether persistence completed successfully. */
    success: boolean;
    /** Human-readable error message if persistence failed. */
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
     * Get an observable of the entity's unique id for reactive updates.
     *
     * A workspace typically registers before it has loaded its entity, so the first
     * {@link extractEntityContext} call reports a null unique and the entity is detected as new. This
     * observable lets the detection re-run once the real id arrives, so the entity key — and
     * everything identified by it — doesn't stay pinned to that pre-load snapshot.
     *
     * Returns undefined if the adapter doesn't support reactive uniques, in which case the key is
     * whatever the initial extract produced.
     */
    getUniqueObservable?(
        workspaceContext: unknown,
    ): import("@umbraco-cms/backoffice/external/rxjs").Observable<string | undefined> | undefined;

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
     * @param activeVariant When provided, overrides variant detection in the adapter so values are
     * read from the correct culture/segment (e.g. the right pane in split-view).
     */
    serializeForLlm(
        workspaceContext: unknown,
        activeVariant?: { culture: string | null; segment: string | null },
    ): Promise<UaiSerializedEntity>;

    /**
     * Apply a value change to the workspace (staged, not persisted).
     * Optional - some entity types may be read-only.
     * @param workspaceContext The workspace context to modify
     * @param change The value change to apply
     * @returns Result indicating success or failure with error message
     */
    applyValueChange?(workspaceContext: unknown, change: UaiValueChange): Promise<UaiValueChangeResult>;

    /**
     * Persist the workspace's staged changes (the equivalent of clicking the workspace's Save
     * button). Optional — entity types that don't own their own save (e.g. blocks, whose changes
     * save with the parent document) should omit this so the caller can return a clear "not
     * supported" error.
     */
    save?(workspaceContext: unknown): Promise<UaiPersistResult>;

    /**
     * Persist the workspace's staged changes and publish them. Optional — only entity types with
     * a publish concept (documents) implement this; media and other always-live entities should
     * omit it.
     */
    publish?(workspaceContext: unknown): Promise<UaiPersistResult>;
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
