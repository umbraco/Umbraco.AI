/**
 * Block Entity Adapter
 *
 * Handles serialization of Umbraco block entities (Block List, Block Grid) for LLM context.
 * Blocks live inside a parent document but have their own workspace context.
 */

import { from, map, of, switchMap, type Observable } from "@umbraco-cms/backoffice/external/rxjs";
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
    /** Always returns '' at runtime — see the {@link name} observable for the real source. */
    getName(): string;
    /**
     * Raw label observable, sourced internally from the block type's configured label markdown (UFM-
     * rendered) — the same text the block list itself shows. Emits undefined until the first render pass
     * completes, and carries a literal, non-localized prefix (`"#general_edit "` or `"#general_add "`,
     * an unresolved localization-key marker, not user-facing text) that must be stripped — see
     * stripBlockNamePrefix. Emits `"#general_edit "` with nothing after it when the block type has no
     * label markdown configured at all.
     */
    readonly name?: Observable<string | undefined>;
    /**
     * Set once UMB_MODAL_CONTEXT resolves — public on UmbSubmittableWorkspaceContextBase, inherited here.
     * This workspace's own host is the block's edit-workspace element, mounted wherever the CMS's
     * routable-workspace machinery puts it — not necessarily anywhere near the block-list/grid entry that
     * provides UmbBlockEntryContext. Confirmed live: requesting UmbBlockEntryContext directly via this
     * object's own getContext() never finds a provider. UmbModalManagerContext.open()'s own doc comment
     * says the invoking host "additionally acts as the modal origin for the context api": UmbModalContext
     * is itself a controller hosted on that original invoker (the block-list/grid property editor that
     * opened this edit workspace), so going through modalContext.getContext(...) reaches a provider this
     * object's own getContext() cannot — confirmed live, this is what actually resolves
     * UmbBlockEntryContext successfully.
     */
    modalContext?: { getContext<T>(alias: string): Promise<T | undefined> };
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
 * Minimal surface of UmbBlockEntryContext this adapter needs. That class is abstract and its concrete
 * per-editor subclasses (block-list, block-grid, block-rte, block-single) are generic enough that CMS
 * core doesn't export a single non-generic token for it — every one of those subclasses registers
 * itself under this same literal alias regardless of editor type (see UMB_BLOCK_LIST_ENTRY_CONTEXT et
 * al., each `new UmbContextToken('UmbBlockEntryContext')`), so requesting the alias directly matches
 * whichever one actually owns this block.
 */
interface BlockEntryContextLike {
    /** Resolved, UFM-rendered plain-text label — the same text the block list itself renders. */
    readonly label: Observable<string>;
}

const BLOCK_ENTRY_CONTEXT_ALIAS = "UmbBlockEntryContext";

/**
 * Observable of the block's real label, sourced from its owning UmbBlockEntryContext (the block-list/
 * grid entry, not the block's own edit-workspace) — see BlockEntryContextLike and
 * BlockWorkspaceContextLike.modalContext for why the lookup has to go through modalContext rather than
 * this workspace context's own getContext(). Emits undefined when no modal context is available (e.g.
 * inline editing mode, which has no modal) or no entry context is reachable through it.
 */
function blockEntryLabel$(ctx: BlockWorkspaceContextLike): Observable<string | undefined> {
    const lookup = ctx.modalContext?.getContext<BlockEntryContextLike>(BLOCK_ENTRY_CONTEXT_ALIAS);
    if (!lookup) return of(undefined);
    // getContext() REJECTS (not resolves undefined) when no provider answers — must be caught, or this
    // whole observable errors out instead of emitting, silently skipping every downstream fallback below.
    return from(lookup.catch(() => undefined)).pipe(switchMap((entry) => entry?.label ?? of(undefined)));
}

/**
 * Literal, non-localized markers UmbBlockWorkspaceContext prepends to its `name` observable's value —
 * see stripBlockNamePrefix.
 */
const BLOCK_NAME_PREFIXES = ["#general_edit ", "#general_add "];

/**
 * Strips UmbBlockWorkspaceContext's `"#general_edit "`/`"#general_add "` marker off its `name`
 * observable's value, leaving the block's actual resolved label. Used only as a fallback when
 * blockEntryLabel$ can't reach the entry context — this value is really a modal-title string that
 * happens to end with the label, not a purpose-built "get the label" API, and would break silently if
 * the CMS ever changes that title's format. Returns undefined for a still-pending emission, an
 * unprefixed value from a non-CMS/mocked context, or a block type with no label markdown configured
 * (whose value is just the bare prefix with nothing after it).
 */
function stripBlockNamePrefix(raw: string | undefined): string | undefined {
    if (!raw) return undefined;
    const prefix = BLOCK_NAME_PREFIXES.find((p) => raw.startsWith(p));
    const stripped = (prefix ? raw.slice(prefix.length) : raw).trim();
    return stripped || undefined;
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
     *
     * Primary source is the owning UmbBlockEntryContext's resolved label (blockEntryLabel$) — the same
     * text the block list itself renders, distinguishing "USP Block" from "CTA Block" and one instance
     * from another. Confirmed live in the backoffice (not just unit-tested against a mock): this requires
     * going through BlockWorkspaceContextLike.modalContext rather than this workspace context's own
     * getContext(), which never finds a provider. Falls back to the workspace context's own `name`
     * observable (stripBlockNamePrefix) when no modal context is available (e.g. inline editing mode),
     * then to the literal "Block" when neither source has anything.
     */
    getNameObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
        const ctx = workspaceContext as BlockWorkspaceContextLike;
        return blockEntryLabel$(ctx).pipe(
            switchMap((entryLabel) => (entryLabel ? of(entryLabel) : (ctx.name?.pipe(map(stripBlockNamePrefix)) ?? of(undefined)))),
            map((label) => label || ctx.getName() || "Block"),
        );
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
