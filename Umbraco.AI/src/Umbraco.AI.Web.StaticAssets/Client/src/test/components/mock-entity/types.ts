import type { UmbPropertyTypeModel } from "@umbraco-cms/backoffice/content-type";

/**
 * A merged container that combines containers from the main type and compositions
 * that share the same name, type, and parent path.
 */
export interface MergedContainer {
    key: string;
    ids: Set<string>;
    name: string;
    type: "Tab" | "Group";
    sortOrder: number;
    parentKey: string | null;
}

export interface GroupViewModel {
    key: string;
    name: string;
    sortOrder: number;
    properties: UmbPropertyTypeModel[];
}

export interface TabViewModel {
    key: string;
    name: string;
    sortOrder: number;
    groups: GroupViewModel[];
    rootProperties: UmbPropertyTypeModel[];
}
