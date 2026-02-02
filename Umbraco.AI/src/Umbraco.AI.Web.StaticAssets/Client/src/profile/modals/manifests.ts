import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

export const profileModalManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "UmbracoAI.Modal.Profile.CreateOptions",
        name: "Profile Create Options Modal",
        element: () => import("./create-options/profile-create-options-modal.element.js"),
    },
];
