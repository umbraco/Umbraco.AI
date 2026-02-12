/**
 * Document Entity Adapter
 *
 * Handles serialization of Umbraco document entities for LLM context.
 * Currently supports TextBox and TextArea property editors only.
 */

import { map, type Observable } from "@umbraco-cms/backoffice/external/rxjs";
import { UmbVariantId } from "@umbraco-cms/backoffice/variant";
import type {
    UaiEntityAdapterApi,
    UaiEntityContext,
    UaiValueChange,
    UaiValueChangeResult,
    UaiSerializedEntity,
    UaiSerializedProperty,
} from "../types.js";

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
    // Structure manager for content type info including icon and property structure
    structure?: {
        ownerContentType?: Observable<{ icon?: string } | undefined>;
        getPropertyStructureByAlias?(alias: string): Promise<PropertyStructure | undefined>;
        getContentTypeProperties?(): Promise<PropertyStructure[]>;
    };
    // Property mutation - sets value in workspace (staged, not persisted)
    setPropertyValue?<T>(alias: string, value: T, variantId?: UmbVariantId): Promise<void>;
    // Check if entity is new (being created)
    getIsNew?(): boolean | undefined;
    // Internal method for parent access when creating new entities
    // Note: Uses internal API as UMB_PARENT_ENTITY_CONTEXT is not reliably available
    // See: https://github.com/umbraco/Umbraco-CMS/issues/21368
    _internal_getCreateUnderParent?(): { entityType: string; unique: string | null } | undefined;
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
     * Uses the variants observable which properly tracks name changes.
     */
    getNameObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;

        // Use variants observable - this properly tracks name changes
        // The variants array contains all variant data including names
        if (ctx.variants) {
            return ctx.variants.pipe(
                map((variants) => {
                    // For invariant documents, there's one variant with culture: null
                    // For variant documents, pick the first one (or we could expose selection later)
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
     * Only includes TextBox and TextArea properties for now.
     */
    async serializeForLlm(workspaceContext: unknown): Promise<UaiSerializedEntity> {
        const ctx = workspaceContext as DocumentWorkspaceContextLike;

        const unique = ctx.getUnique();
        const name = ctx.getName() ?? "Untitled";
        const contentType = ctx.getContentTypeUnique();
        const values = ctx.getValues() ?? [];

        // Get parent unique for new documents
        const isNew = ctx.getIsNew?.();
        const parentUnique = isNew ? ctx._internal_getCreateUnderParent?.()?.unique : undefined;

        // Build maps for quick lookup
        // Map: alias -> value entry (for getting current value and editorAlias)
        const valuesByAlias = new Map(values.map((v) => [v.alias, v]));
        // Map: dataType.unique -> editorAlias (for properties without values)
        const editorAliasByDataType = new Map<string, string>();
        for (const v of values) {
            // Get the property structure to find its dataType.unique
            const structure = await ctx.structure?.getPropertyStructureByAlias?.(v.alias);
            if (structure?.dataType.unique) {
                editorAliasByDataType.set(structure.dataType.unique, v.editorAlias);
            }
        }

        // Get all properties from structure
        const propertyStructures = (await ctx.structure?.getContentTypeProperties?.()) ?? [];

        const properties: UaiSerializedProperty[] = [];

        for (const prop of propertyStructures) {
            const valueEntry = valuesByAlias.get(prop.alias);

            // Determine editor alias: from value entry, or from dataType mapping
            const editorAlias = valueEntry?.editorAlias ?? editorAliasByDataType.get(prop.dataType.unique);

            // Only include if we know it's a supported editor
            if (editorAlias) {
                // && SUPPORTED_EDITOR_ALIASES.includes(editorAlias)) {
                properties.push({
                    alias: prop.alias,
                    label: prop.name,
                    editorAlias,
                    value: valueEntry?.value ?? null,
                });
            }
        }

        // Fallback: if we couldn't get properties from structure, use values directly
        if (propertyStructures.length === 0 && values.length > 0) {
            for (const v of values) {
                properties.push({
                    alias: v.alias,
                    label: v.alias,
                    editorAlias: v.editorAlias,
                    value: v.value,
                });
            }
        }

        return {
            entityType: "document",
            unique: unique ?? "new",
            name,
            parentUnique,
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

        // Handle specific type conversions if needed
        let valueToSet: any = change.value;

        try {
            valueToSet = JSON.parse(valueToSet);
            if (existingValue && existingValue.editorAlias === "Umbraco.MediaPicker3") {
                valueToSet[0].key = this.#uuidv4();
            }
        } catch (e) {}

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

    #uuidv4 = () => {
        return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, (c) =>
            (+c ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (+c / 4)))).toString(16),
        );
    };
}

export default UaiDocumentAdapter;
