import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

export const resourceListModalManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "Uai.Modal.ContextResourceTypePicker",
        name: "Context Resource Type Picker Modal",
        element: () => import("./context-resource-type-picker-modal.element.js"),
    },
    {
        type: "modal",
        alias: "Uai.Modal.ResourceOptions",
        name: "Resource Options Modal",
        element: () => import("./resource-options-modal.element.js"),
    },
];
