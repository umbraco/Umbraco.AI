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
    UaiValueChange,
    UaiValueChangeResult,
    UaiSerializedEntity,
    UaiSerializedProperty,
} from "../types.js";
import { resolveAndPrepareValue } from "../value-preparers/resolver.js";
import { resolveEditorSchemaAlias } from "../resolve-editor-schema-alias.js";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { pickValueForVariant, type ActiveVariantInfo } from "./variant-selection.js";

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
    /** The variant the block is being edited in (inherited from parent doc). */
    getVariantId?(): { culture: string | null; segment: string | null } | undefined;
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
 * Read the variant the block is being edited in. Returns null when the block
 * lives on an invariant document (or when the API isn't exposed on the mock
 * workspace).
 */
function getActiveVariant(ctx: BlockWorkspaceContextLike): ActiveVariantInfo | null {
    const variantId = ctx.getVariantId?.();
    if (!variantId) return null;
    return { culture: variantId.culture ?? null, segment: variantId.segment ?? null };
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
     *
     * Inherits variant context from the parent document so prompt template
     * variables resolve to the active culture's value when the block lives in
     * a multi-variant document.
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
        const active = getActiveVariant(ctx);

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
            const structure = await ctx.content.structure?.getPropertyStructureByAlias?.(alias);
            if (structure?.dataType.unique) {
                editorAliasByDataType.set(structure.dataType.unique, entries[0].editorAlias);
            }
        }

        // Get all properties from structure
        const propertyStructures = (await ctx.content.structure?.getContentTypeProperties?.()) ?? [];

        const properties: UaiSerializedProperty[] = [];

        for (const prop of propertyStructures) {
            const valueEntry = pickValueForVariant(valuesByAlias.get(prop.alias) ?? [], active);
            const editorAlias = valueEntry?.editorAlias ?? editorAliasByDataType.get(prop.dataType.unique);

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
        // active-variant entries so the fallback path also respects culture.
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
            entityType: "block",
            unique: unique ?? "new",
            name,
            culture: active?.culture ?? null,
            segment: active?.segment ?? null,
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

        // Prepare value for the target editor type. Resolve the editor alias (falling back to the
        // data type when the field is empty and has no existing value entry) so preparers still run.
        const editorAlias = await resolveEditorSchemaAlias(
            ctx as unknown as UmbControllerHost, existingValue?.editorAlias, property?.dataType?.unique);
        const valueToSet = await resolveAndPrepareValue(change.value, editorAlias, existingValue?.value);

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
