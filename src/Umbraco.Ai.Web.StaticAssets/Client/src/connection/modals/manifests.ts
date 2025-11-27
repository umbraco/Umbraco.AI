import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

export const connectionModalManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "UmbracoAi.Modal.Connection.CreateOptions",
        name: "Connection Create Options Modal",
        element: () => import("./create-options/connection-create-options-modal.element.js"),
    },
];
