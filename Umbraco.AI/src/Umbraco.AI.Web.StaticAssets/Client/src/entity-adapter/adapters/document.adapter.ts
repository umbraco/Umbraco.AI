/**
 * Document Entity Adapter
 *
 * Handles serialization of Umbraco document entities for LLM context.
 * Currently supports TextBox and TextArea property editors only.
 */

import { map, type Observable } from "@umbraco-cms/backoffice/external/rxjs";
import { UmbVariantId } from "@umbraco-cms/backoffice/variant";
import { UMB_DOCUMENT_PUBLISHING_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/document";
import type {
    UaiEntityAdapterApi,
    UaiEntityContext,
    UaiPersistResult,
    UaiValueChange,
    UaiValueChangeResult,
    UaiSerializedEntity,
    UaiSerializedProperty,
} from "../types.js";
import { resolveAndPrepareValue } from "../value-preparers/resolver.js";
import { pickValueForVariant, type ActiveVariantInfo } from "./variant-selection.js";

// Supported text-based property editors for initial implementation
// const SUPPORTED_EDITOR_ALIASES = ["Umbraco.TextBox", "Umbraco.TextArea"];

/**
 * Property structure from content type.
 */
interface PropertyStructure {
    alias: string;
    name: string;
    description?: string | null;
    dataType: { unique: string };
}

/**
 * Interface matching the essential methods of UmbDocumentWorkspaceContext.
 * We use duck-typing rather than importing the actual type to avoid tight coupling.
 */
interface DocumentWorkspaceContextLike {
    getEntityType(): string;
    getUnique(): string | undefined;
    getName(variantId?: unknown): string | undefined;
    getContentTypeUnique(): string | undefined;
    getValues():
        | Array<{
              alias: string;
              value?: unknown;
              culture: string | null;
              segment: string | null;
              editorAlias: string;
          }>
        | undefined;
    // name() is a METHOD that returns Observable<string> (not a property!)
    name?(variantId?: unknown): Observable<string>;
    // variants observable - contains name for each variant
    variants?: Observable<Array<{ name?: string; culture?: string | null }>>;
    // Split-view manager exposing the variants currently focused in the editor.
    // For a single-pane edit there is one entry; in split view there can be more.
    splitView?: {
        getActiveVariants?(): ActiveVariantInfo[] | undefined;
        activeVariantsInfo?: Observable<ActiveVariantInfo[] | undefined>;
    };
    // Structure manager for content type info including icon and property structure
    structure?: {
        ownerContentType?: Observable<{ icon?: string } | undefined>;
        getPropertyStructureByAlias?(alias: string): Promise<PropertyStructure | undefined>;
        getContentTypeProperties?(): Promise<PropertyStructure[]>;
    };
    // Property mutation - sets value in workspace (staged, not persisted)
    setPropertyValue?<T>(alias: string, value: T, variantId?: UmbVariantId): Promise<void>;
    // Persists staged changes (clicking the workspace Save button). Throws on validation failure.
    requestSubmit?(): Promise<void>;
    // The workspace context is itself a UmbControllerBase so it can resolve sibling contexts
    // registered against the same host element — used to fetch the publishing context.
    getContext?<T>(token: unknown): Promise<T | undefined>;
    // Check if entity is new (being created)
    getIsNew?(): boolean | undefined;
    // Internal method for parent access when creating new entities
    // Note: Uses internal API as UMB_PARENT_ENTITY_CONTEXT is not reliably available
    // See: https://github.com/umbraco/Umbraco-CMS/issues/21368
    _internal_getCreateUnderParent?(): { entityType: string; unique: string | null } | undefined;
}

/**
 * Resolve the active variant from the workspace's split-view manager.
 * Returns null when none can be determined (invariant document, missing API,
 * mocked context). Falls back to the first active variant when the caller
 * does not supply an override (single-pane editing, invariant content).
 */
function getActiveVariant(ctx: DocumentWorkspaceContextLike): ActiveVariantInfo | null {
    const active = ctx.splitView?.getActiveVariants?.();
    if (active && active.length > 0) {
        return { culture: active[0].culture ?? null, segment: active[0].segment ?? null };
    }
    return null;
}

/**
 * Adapter for Umbraco document entities.
 */
export class UaiDocumentAdapter implements UaiEntityAdapterApi {
    readonly entityType = "document";

    /**
     * Check if the workspace context is a document workspace.
     * Uses duck-typing to check for document-specific methods.
     */
    canHandle(workspaceContext: unknown): boolean {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;
        return (
            typeof ctx?.getEntityType === "function" &&
            ctx.getEntityType() === "document" &&
            typeof ctx?.getUnique === "function" &&
            typeof ctx?.getName === "function"
        );
    }

    /**
     * Extract entity identity from document workspace context.
     */
    extractEntityContext(workspaceContext: unknown): UaiEntityContext {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;
        return {
            entityType: "document",
            unique: ctx.getUnique() ?? null,
        };
    }

    /**
     * Get the current display name for the document.
     */
    getName(workspaceContext: unknown): string {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;
        return ctx.getName() ?? "Untitled";
    }

    /**
     * Get an observable for the document name for reactive updates.
     * Uses the variants observable which properly tracks name changes, and
     * picks the variant whose culture matches what the editor currently has
     * focused so the AI context selector shows the right name on multi-
     * variant documents.
     */
    getNameObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;

        // Use variants observable - this properly tracks name changes.
        // Read the active variant fresh on every emission so name updates
        // reflect both edit-time changes and split-view focus changes.
        if (ctx.variants) {
            return ctx.variants.pipe(
                map((variants) => {
                    const active = getActiveVariant(ctx);
                    if (active) {
                        const match = variants.find((v) => (v.culture ?? null) === active.culture);
                        if (match?.name) return match.name;
                    }
                    const invariantVariant = variants.find((v) => v.culture === null);
                    return invariantVariant?.name ?? variants[0]?.name;
                }),
            );
        }

        // Fallback: try name() method directly
        if (typeof ctx.name === "function") {
            try {
                return ctx.name();
            } catch {
                return undefined;
            }
        }

        return undefined;
    }

    /**
     * Get the icon for the document from its content type.
     * Returns undefined initially - use getIconObservable for reactive updates.
     */
    getIcon(workspaceContext: unknown): string | undefined {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;
        // We can't synchronously get the icon since it comes from the structure manager
        // which is async. Return undefined and let the observable handle it.
        // Could also try to access a sync getter if available:
        const structure = ctx.structure as { getOwnerContentType?: () => { icon?: string } | undefined };
        if (typeof structure?.getOwnerContentType === "function") {
            return structure.getOwnerContentType()?.icon;
        }
        return undefined;
    }

    /**
     * Get an observable for the document icon for reactive updates.
     * Uses the structure manager's ownerContentType observable to get the icon.
     */
    getIconObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;

        // Get icon from content type structure manager
        if (ctx.structure?.ownerContentType) {
            return ctx.structure.ownerContentType.pipe(map((ct: { icon?: string } | undefined) => ct?.icon));
        }

        return undefined;
    }

    /**
     * Serialize document for LLM context.
     * Uses structure to get all properties, then merges with values.
     *
     * Multi-variant content: `getValues()` returns one entry per
     * (alias, culture, segment). We pick the entry that matches the variant
     * the editor currently has focused, falling back to the invariant entry
     * for properties that don't vary, so prompt template variables like
     * `{{header}}` resolve to the active culture's value.
     */
    async serializeForLlm(
        workspaceContext: unknown,
        activeVariant?: { culture: string | null; segment: string | null },
    ): Promise<UaiSerializedEntity> {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;

        const unique = ctx.getUnique();
        const contentType = ctx.getContentTypeUnique();
        const values = ctx.getValues() ?? [];
        const active = activeVariant ?? getActiveVariant(ctx);

        // Pick the active variant's name when available so the LLM sees the
        // name from the variant the editor is on, matching the property values.
        const name = ctx.getName(active ? new UmbVariantId(active.culture, active.segment) : undefined) ?? "Untitled";

        // Get parent unique for new documents
        const isNew = ctx.getIsNew?.();
        const parentUnique = isNew ? ctx._internal_getCreateUnderParent?.()?.unique : undefined;

        // Group values by alias so we can pick the active-variant entry per property.
        // On multi-variant content `values` has N×M entries (cultures × properties);
        // grouping by alias also lets us look up each property's structure once
        // instead of per-culture.
        const valuesByAlias = new Map<string, typeof values>();
        for (const v of values) {
            const bucket = valuesByAlias.get(v.alias);
            if (bucket) {
                bucket.push(v);
            } else {
                valuesByAlias.set(v.alias, [v]);
            }
        }

        // Map: dataType.unique -> editorAlias (for properties without values).
        // One structure lookup per unique alias, not per (alias × culture).
        const editorAliasByDataType = new Map<string, string>();
        for (const [alias, entries] of valuesByAlias) {
            const structure = await ctx.structure?.getPropertyStructureByAlias?.(alias);
            if (structure?.dataType.unique) {
                editorAliasByDataType.set(structure.dataType.unique, entries[0].editorAlias);
            }
        }

        // Get all properties from structure
        const propertyStructures = (await ctx.structure?.getContentTypeProperties?.()) ?? [];

        const properties: UaiSerializedProperty[] = [];

        for (const prop of propertyStructures) {
            const valueEntry = pickValueForVariant(valuesByAlias.get(prop.alias) ?? [], active);

            // Determine editor alias: from value entry, or from dataType mapping
            const editorAlias = valueEntry?.editorAlias ?? editorAliasByDataType.get(prop.dataType.unique);

            // Only include if we know it's a supported editor
            if (editorAlias) {
                properties.push({
                    alias: prop.alias,
                    label: prop.name,
                    editorAlias,
                    value: valueEntry?.value ?? null,
                    culture: valueEntry?.culture ?? null,
                    segment: valueEntry?.segment ?? null,
                });
            }
        }

        // Fallback: if we couldn't get properties from structure, use the
        // already-filtered active-variant entries so the fallback path also
        // respects the active culture.
        if (propertyStructures.length === 0 && values.length > 0) {
            for (const [alias, entries] of valuesByAlias) {
                const v = pickValueForVariant(entries, active);
                if (!v) continue;
                properties.push({
                    alias,
                    label: alias,
                    editorAlias: v.editorAlias,
                    value: v.value,
                    culture: v.culture,
                    segment: v.segment,
                });
            }
        }

        return {
            entityType: "document",
            unique: unique ?? "new",
            name,
            parentUnique,
            culture: active?.culture ?? null,
            segment: active?.segment ?? null,
            data: {
                contentType: contentType ?? undefined,
                properties,
            },
        };
    }

    /**
     * Apply a value change to the document workspace.
     * Changes are staged in the workspace - user must save to persist.
     * For CMS documents, the path is treated as the property alias.
     * Only supports text-based properties (TextBox, TextArea) for now.
     */
    async applyValueChange(workspaceContext: unknown, change: UaiValueChange): Promise<UaiValueChangeResult> {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;

        // Check if workspace supports property mutation
        if (typeof ctx.setPropertyValue !== "function") {
            return {
                success: false,
                error: "Workspace does not support property mutation",
            };
        }

        // For CMS documents, treat path as property alias
        const propertyAlias = change.path;

        // Validate property exists and is a supported type
        const property = await ctx.structure?.getPropertyStructureByAlias?.(propertyAlias);
        if (!property) {
            return {
                success: false,
                error: `Property "${propertyAlias}" not found on this document type`,
            };
        }

        // Get the current values to check the editor type
        const values = ctx.getValues() ?? [];
        const existingValue = values.find((v) => v.alias === propertyAlias);

        // Build variant ID from culture/segment (undefined = invariant)
        const variantId = new UmbVariantId(change.culture ?? null, change.segment ?? null);

        // Prepare value for the target editor type
        const valueToSet = await resolveAndPrepareValue(change.value, existingValue?.editorAlias, existingValue?.value);

        try {
            await ctx.setPropertyValue(propertyAlias, valueToSet, variantId);
            return { success: true };
        } catch (error) {
            return {
                success: false,
                error: error instanceof Error ? error.message : "Unknown error applying value change",
            };
        }
    }

    /**
     * Persist staged changes to the document (equivalent to clicking Save).
     * Delegates to the workspace's `requestSubmit()`; any validation failure thrown by the
     * workspace is converted into a structured error so the LLM can read it.
     */
    async save(workspaceContext: unknown): Promise<UaiPersistResult> {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;

        if (typeof ctx.requestSubmit !== "function") {
            return { success: false, error: "Workspace does not support save (no requestSubmit on context)." };
        }

        try {
            await ctx.requestSubmit();
            return { success: true };
        } catch (error) {
            return {
                success: false,
                error: error instanceof Error ? error.message : "Save failed (workspace rejected the request).",
            };
        }
    }

    /**
     * Save the document and publish it. Resolves the document publishing workspace context as a
     * sibling of the workspace context (both register against the same host) and delegates to
     * `saveAndPublish()`. On multi-variant documents this can surface the CMS's variant-picker
     * modal — same UX as clicking the Save and publish button.
     */
    async publish(workspaceContext: unknown): Promise<UaiPersistResult> {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;

        if (typeof ctx.getContext !== "function") {
            return {
                success: false,
                error: "Workspace context does not expose getContext; cannot reach the publishing context.",
            };
        }

        type PublishingCtxLike = { saveAndPublish?: () => Promise<void> };
        const publishing = await ctx.getContext<PublishingCtxLike>(UMB_DOCUMENT_PUBLISHING_WORKSPACE_CONTEXT);

        if (!publishing || typeof publishing.saveAndPublish !== "function") {
            return {
                success: false,
                error: "Document publishing context is not available on this workspace.",
            };
        }

        try {
            await publishing.saveAndPublish();
            return { success: true };
        } catch (error) {
            return {
                success: false,
                error: error instanceof Error ? error.message : "Publish failed (publishing context rejected the request).",
            };
        }
    }

    /**
     * Cleanup method required by UmbApi base type.
     * Currently no resources to clean up as the adapter is stateless.
     */
    destroy(): void {
        // No cleanup needed - adapter maintains no subscriptions or resources
    }
}

export default UaiDocumentAdapter;
