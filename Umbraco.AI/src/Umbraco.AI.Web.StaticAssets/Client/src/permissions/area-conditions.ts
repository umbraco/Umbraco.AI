import { UMB_FALLBACK_USER_PERMISSION_CONDITION_ALIAS } from "@umbraco-cms/backoffice/user-permission";
import { UAI_REGISTERED_GROUP_PERMISSION_CONDITION_ALIAS } from "./constants.js";
import { verbsInGroup } from "./manifests.js";

/**
 * Manifest types that open or change an area (its menu item, lists, editors and the buttons and
 * actions on them). Only these get the area's right. Everything else a feature registers stays open
 * on purpose: modals, repositories, stores, pickers and the views that hang off a gated workspace or
 * collection, so a picker in another editor keeps working without the right.
 */
export const UAI_AREA_GATED_MANIFEST_TYPES: ReadonlySet<string> = new Set([
    "menuItem",
    "collection",
    "collectionAction",
    "entityBulkAction",
    "entityAction",
    "entityCreateOptionAction",
    "workspace",
    "workspaceAction",
]);

/** The condition that shows an extension only to users whose groups hold the area's right. */
export function createAreaPermissionCondition(verb: string) {
    return { alias: UMB_FALLBACK_USER_PERMISSION_CONDITION_ALIAS, allOf: [verb] };
}

/** The condition that shows an extension to users who hold any right in a permission group. */
export function createGroupPermissionCondition(group: string) {
    const verbs = verbsInGroup(group);
    // The CMS skips an empty oneOf, which would show the extension to everyone.
    if (verbs.length === 0) throw new Error(`Permission group "${group}" has no verbs to condition on.`);
    return { alias: UMB_FALLBACK_USER_PERMISSION_CONDITION_ALIAS, oneOf: verbs };
}

/**
 * The condition that shows an extension to users who hold any right registered in a group, wherever
 * it was registered from. Use it for a group core keeps no list of, like Add-ons.
 */
export function createRegisteredGroupPermissionCondition(group: string) {
    return { alias: UAI_REGISTERED_GROUP_PERMISSION_CONDITION_ALIAS, group };
}

/**
 * Adds the area's right as a condition to every gated-type manifest in the list, keeping any
 * conditions it already has. Wrap each area's manifest arrays with this where they are aggregated,
 * so a manifest added later is covered without touching a list.
 */
export function withAreaPermission<T extends { type: string }>(verb: string, manifests: ReadonlyArray<T>): Array<T> {
    return manifests.map((manifest) => {
        if (!UAI_AREA_GATED_MANIFEST_TYPES.has(manifest.type)) return manifest;
        const existing = (manifest as { conditions?: Array<unknown> }).conditions ?? [];
        return { ...manifest, conditions: [...existing, createAreaPermissionCondition(verb)] };
    });
}
