/**
 * Block Entity Adapter
 *
 * Handles serialization of Umbraco block entities (Block List, Block Grid) for LLM context.
 * Blocks live inside a parent document but have their own workspace context.
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
import { prepareValueForEditor } from "./value-preparation.js";

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
 * Interface matching the essential methods/properties of UmbBlockWorkspaceContext.
 * We use duck-typing with IS_BLOCK_WORKSPACE_CONTEXT as a reliable marker.
 */
interface BlockWorkspaceContextLike {
    IS_BLOCK_WORKSPACE_CONTEXT: true;
    getUnique(): string;
    getEntityType(): string;
    getName(): string;
    content: {
        getValues():
            | Array<{
                  alias: string;
                  value?: unknown;
                  culture: string | null;
                  segment: string | null;
                  editorAlias: string;
              }>
            | undefined;
        getData(): { contentTypeKey?: string; key?: string } | undefined;
        setPropertyValue?<T>(alias: string, value: T, variantId?: UmbVariantId): Promise<void>;
        name?: Observable<string | undefined>;
        structure: {
            ownerContentType?: Observable<{ alias?: string; icon?: string } | undefined>;
            contentTypeAliases?: Observable<string[]>;
            getPropertyStructureByAlias?(alias: string): Promise<PropertyStructure | undefined>;
            getContentTypeProperties?(): Promise<PropertyStructure[]>;
        };
    };
}

/**
 * Adapter for Umbraco block entities (Block List, Block Grid items).
 */
export class UaiBlockAdapter implements UaiEntityAdapterApi {
    readonly entityType = "block";

    /**
     * Check if the workspace context is a block workspace.
     * Uses IS_BLOCK_WORKSPACE_CONTEXT as a reliable duck-typing marker.
     */
    canHandle(workspaceContext: unknown): boolean {
        const ctx = workspaceContext as BlockWorkspaceContextLike;
        return ctx?.IS_BLOCK_WORKSPACE_CONTEXT === true;
    }

    /**
     * Extract entity identity from block workspace context.
     */
    extractEntityContext(workspaceContext: unknown): UaiEntityContext {
        const ctx = workspaceContext as BlockWorkspaceContextLike;
        let unique: string | null = null;
        try {
            unique = ctx.getUnique() ?? null;
        } catch {
            // getUnique() can throw if contentKey is not yet available
        }
        return {
            entityType: "block",
            unique,
        };
    }

    /**
     * Get the current display name for the block.
     */
    getName(workspaceContext: unknown): string {
        const ctx = workspaceContext as BlockWorkspaceContextLike;
        return ctx.getName() || "Block";
    }

    /**
     * Get an observable for the block name for reactive updates.
     * Uses the content element manager's name observable.
     */
    getNameObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
        const ctx = workspaceContext as BlockWorkspaceContextLike;
        if (ctx.content?.name) {
            return ctx.content.name.pipe(map((name) => name || "Block"));
        }
        return undefined;
    }

    /**
     * Get the icon for the block from its content type.
     */
    getIcon(_workspaceContext: unknown): string | undefined {
        return undefined;
    }

    /**
     * Get an observable for the block icon for reactive updates.
     */
    getIconObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
        const ctx = workspaceContext as BlockWorkspaceContextLike;
        if (ctx.content?.structure?.ownerContentType) {
            return ctx.content.structure.ownerContentType.pipe(
                map((ct: { icon?: string } | undefined) => ct?.icon),
            );
        }
        return undefined;
    }

    /**
     * Serialize block for LLM context.
     * Uses the content element manager's structure to get properties and values.
     */
    async serializeForLlm(workspaceContext: unknown): Promise<UaiSerializedEntity> {
        const ctx = workspaceContext as BlockWorkspaceContextLike;

        let unique: string | undefined;
        try {
            unique = ctx.getUnique();
        } catch {
            // getUnique() can throw if contentKey is not yet available
        }
        const name = ctx.getName() || "Block";
        const contentData = ctx.content.getData();
        const contentTypeKey = contentData?.contentTypeKey;
        const values = ctx.content.getValues() ?? [];

        // Build map for quick lookup: alias -> value entry
        const valuesByAlias = new Map(values.map((v) => [v.alias, v]));
        // Map: dataType.unique -> editorAlias (for properties without values)
        const editorAliasByDataType = new Map<string, string>();
        for (const v of values) {
            const structure = await ctx.content.structure?.getPropertyStructureByAlias?.(v.alias);
            if (structure?.dataType.unique) {
                editorAliasByDataType.set(structure.dataType.unique, v.editorAlias);
            }
        }

        // Get all properties from structure
        const propertyStructures = (await ctx.content.structure?.getContentTypeProperties?.()) ?? [];

        const properties: UaiSerializedProperty[] = [];

        for (const prop of propertyStructures) {
            const valueEntry = valuesByAlias.get(prop.alias);
            const editorAlias = valueEntry?.editorAlias ?? editorAliasByDataType.get(prop.dataType.unique);

            if (editorAlias) {
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
            entityType: "block",
            unique: unique ?? "new",
            name,
            data: {
                contentType: contentTypeKey ?? undefined,
                properties,
            },
        };
    }

    /**
     * Apply a value change to the block workspace.
     * Changes are staged in the workspace - user must save to persist.
     */
    async applyValueChange(workspaceContext: unknown, change: UaiValueChange): Promise<UaiValueChangeResult> {
        const ctx = workspaceContext as BlockWorkspaceContextLike;

        if (typeof ctx.content.setPropertyValue !== "function") {
            return {
                success: false,
                error: "Block workspace does not support property mutation",
            };
        }

        const propertyAlias = change.path;

        // Validate property exists
        const property = await ctx.content.structure?.getPropertyStructureByAlias?.(propertyAlias);
        if (!property) {
            return {
                success: false,
                error: `Property "${propertyAlias}" not found on this block element type`,
            };
        }

        // Build variant ID from culture/segment
        const variantId = new UmbVariantId(change.culture ?? null, change.segment ?? null);

        // Get the current value to determine editor type for value preparation
        const values = ctx.content.getValues() ?? [];
        const existingValue = values.find((v) => v.alias === propertyAlias);

        // Prepare value for the target editor type
        const valueToSet = prepareValueForEditor(change.value, existingValue?.editorAlias, existingValue?.value);

        try {
            await ctx.content.setPropertyValue(propertyAlias, valueToSet, variantId);
            return { success: true };
        } catch (error) {
            return {
                success: false,
                error: error instanceof Error ? error.message : "Unknown error applying value change",
            };
        }
    }

    /**
     * Cleanup method required by UmbApi base type.
     */
    destroy(): void {
        // No cleanup needed - adapter is stateless
    }
}

export default UaiBlockAdapter;
