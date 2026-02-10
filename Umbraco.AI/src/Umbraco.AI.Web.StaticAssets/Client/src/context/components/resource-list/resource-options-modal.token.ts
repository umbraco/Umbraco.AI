import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiContextResourceInjectionMode } from "../../types.js";
import type { UaiContextResourceTypeItemModel } from "../../../context-resource-type/types.js";

export interface UaiResourceOptionsData {
    name: string;
    description: string | null;
    data: Record<string, unknown>;
    injectionMode: UaiContextResourceInjectionMode;
}

export interface UaiResourceOptionsModalData {
    resourceType?: UaiContextResourceTypeItemModel;
    resource?: UaiResourceOptionsData;
}

export interface UaiResourceOptionsModalValue {
    resource: UaiResourceOptionsData;
}

export const UAI_RESOURCE_OPTIONS_MODAL = new UmbModalToken<UaiResourceOptionsModalData, UaiResourceOptionsModalValue>(
    "Uai.Modal.ResourceOptions",
    {
        modal: {
            type: "sidebar",
            size: "medium",
        },
    },
);
