/**
 * Media Entity Adapter
 *
 * Handles serialization of Umbraco media entities for LLM context.
 * Media items are always invariant (no cultures/segments) and the display
 * name lives in the single invariant variant rather than on a dedicated
 * getName() method as documents have.
 *
 * Binary property editors (image cropper, file upload) are rejected by
 * applyValueChange with a clear error - the LLM can still propose changes
 * to text-based properties such as alt text on the image cropper's metadata.
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
 * Property editor aliases that represent the binary payload of a media item.
 * The LLM cannot meaningfully produce these, so we reject writes early with
 * a descriptive error rather than letting a nonsense value flow through
 * prepareValueForEditor.
 */
const BINARY_EDITOR_ALIASES = new Set(["Umbraco.UploadField", "Umbraco.ImageCropper"]);

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
 * Variant entry in the media workspace's variants observable/data.
 * Media is always invariant so this array contains a single entry with
 * culture === null and segment === null.
 */
interface MediaVariantLike {
    name?: string;
    culture?: string | null;
    segment?: string | null;
}

/**
 * Interface matching the essential methods of UmbMediaWorkspaceContext.
 * We use duck-typing rather than importing the actual type to avoid tight
 * coupling to CMS internals.
 */
interface MediaWorkspaceContextLike {
    getEntityType(): string;
    getUnique(): string | undefined;
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
    // Media has no getName(); name lives on variants[0]. We snapshot via getData()
    // for the synchronous getName(), and subscribe via variants observable for reactive updates.
    getData?(): { variants?: MediaVariantLike[] } | undefined;
    variants?: Observable<MediaVariantLike[]>;
    // Direct icon observable available on media; simpler than mapping through structure.ownerContentType.
    contentTypeIcon?: Observable<string | undefined>;
    // Structure manager for content type info including icon and property structure
    structure?: {
        ownerContentType?: Observable<{ icon?: string } | undefined>;
        getPropertyStructureByAlias?(alias: string): Promise<PropertyStructure | undefined>;
        getContentTypeProperties?(): Promise<PropertyStructure[]>;
    };
    // Property mutation - sets value in workspace (staged, not persisted).
    // Media is always invariant but the method still accepts a variantId for
    // consistency with the shared UmbContentDetailWorkspaceContextBase.
    setPropertyValue?<T>(alias: string, value: T, variantId?: UmbVariantId): Promise<void>;
    // Check if entity is new (being created)
    getIsNew?(): boolean | undefined;
    // Internal method for parent access when creating new entities.
    // See https://github.com/umbraco/Umbraco-CMS/issues/21368 for why this is internal.
    _internal_getCreateUnderParent?(): { entityType: string; unique: string | null } | undefined;
}

/**
 * Adapter for Umbraco media entities.
 */
export class UaiMediaAdapter implements UaiEntityAdapterApi {
    readonly entityType = "media";

    /**
     * Check if the workspace context is a media workspace.
     * Uses duck-typing on getEntityType() === "media" plus the core
     * identity/value methods we rely on. Note we do NOT require getName()
     * here the way the document adapter does — media workspaces expose
     * the name through the variants array instead.
     */
    canHandle(workspaceContext: unknown): boolean {
        const ctx = workspaceContext as MediaWorkspaceContextLike;
        return (
            typeof ctx?.getEntityType === "function" &&
            ctx.getEntityType() === "media" &&
            typeof ctx?.getUnique === "function" &&
            typeof ctx?.getValues === "function"
        );
    }

    /**
     * Extract entity identity from media workspace context.
     */
    extractEntityContext(workspaceContext: unknown): UaiEntityContext {
        const ctx = workspaceContext as MediaWorkspaceContextLike;
        return {
            entityType: "media",
            unique: ctx.getUnique() ?? null,
        };
    }

    /**
     * Get the current display name for the media item.
     * Media has no getName() method — the name lives in the single
     * invariant variant on the workspace data model.
     */
    getName(workspaceContext: unknown): string {
        const ctx = workspaceContext as MediaWorkspaceContextLike;
        const variants = ctx.getData?.()?.variants ?? [];
        return variants[0]?.name ?? "Untitled";
    }

    /**
     * Get an observable for the media name for reactive updates.
     * Uses the variants observable so name edits are reflected live.
     */
    getNameObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
        const ctx = workspaceContext as MediaWorkspaceContextLike;
        if (ctx.variants) {
            return ctx.variants.pipe(map((variants) => variants[0]?.name));
        }
        return undefined;
    }

    /**
     * Get the icon for the media item from its media type.
     * Returns undefined initially - getIconObservable is the reactive source.
     */
    getIcon(_workspaceContext: unknown): string | undefined {
        // Icons are only available asynchronously through observables.
        return undefined;
    }

    /**
     * Get an observable for the media icon for reactive updates.
     * Prefers the dedicated contentTypeIcon observable when present,
     * falling back to structure.ownerContentType for older CMS versions.
     */
    getIconObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
        const ctx = workspaceContext as MediaWorkspaceContextLike;

        if (ctx.contentTypeIcon) {
            return ctx.contentTypeIcon;
        }

        if (ctx.structure?.ownerContentType) {
            return ctx.structure.ownerContentType.pipe(map((ct: { icon?: string } | undefined) => ct?.icon));
        }

        return undefined;
    }

    /**
     * Serialize media for LLM context.
     * Produces the same `{ entityType, unique, name, data: { contentType, properties }, parentUnique? }`
     * shape the server-side SerializedEntityContributor looks for.
     */
    async serializeForLlm(workspaceContext: unknown): Promise<UaiSerializedEntity> {
        const ctx = workspaceContext as MediaWorkspaceContextLike;

        const unique = ctx.getUnique();
        const name = this.getName(workspaceContext);
        const contentType = ctx.getContentTypeUnique();
        const values = ctx.getValues() ?? [];

        // Get parent unique for new media items (e.g., uploaded into a folder)
        const isNew = ctx.getIsNew?.();
        const parentUnique = isNew ? ctx._internal_getCreateUnderParent?.()?.unique : undefined;

        // Build maps for quick lookup
        const valuesByAlias = new Map(values.map((v) => [v.alias, v]));
        const editorAliasByDataType = new Map<string, string>();
        for (const v of values) {
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
            entityType: "media",
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
     * Apply a value change to the media workspace.
     * Changes are staged in the workspace - user must save to persist.
     * Binary property editors (upload field, image cropper) are rejected
     * since the LLM cannot meaningfully produce their payloads.
     */
    async applyValueChange(workspaceContext: unknown, change: UaiValueChange): Promise<UaiValueChangeResult> {
        const ctx = workspaceContext as MediaWorkspaceContextLike;

        if (typeof ctx.setPropertyValue !== "function") {
            return {
                success: false,
                error: "Workspace does not support property mutation",
            };
        }

        const propertyAlias = change.path;

        // Validate the property exists on this media type
        const property = await ctx.structure?.getPropertyStructureByAlias?.(propertyAlias);
        if (!property) {
            return {
                success: false,
                error: `Property "${propertyAlias}" not found on this media type`,
            };
        }

        const values = ctx.getValues() ?? [];
        const existingValue = values.find((v) => v.alias === propertyAlias);

        // Reject attempts to overwrite the binary file itself.
        if (existingValue?.editorAlias && BINARY_EDITOR_ALIASES.has(existingValue.editorAlias)) {
            return {
                success: false,
                error: `Property "${propertyAlias}" uses editor "${existingValue.editorAlias}" which stores binary media and cannot be set from an AI response`,
            };
        }

        // Media is always invariant; culture/segment on the change are ignored
        // but we still pass them through so any future media variant support
        // surfaces the right variant id without changes here.
        const variantId = new UmbVariantId(change.culture ?? null, change.segment ?? null);

        const valueToSet = prepareValueForEditor(change.value, existingValue?.editorAlias, existingValue?.value);

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
     * Cleanup method required by UmbApi base type.
     * Adapter is stateless; no subscriptions to release.
     */
    destroy(): void {
        // No cleanup needed - adapter maintains no subscriptions or resources
    }
}

export default UaiMediaAdapter;
