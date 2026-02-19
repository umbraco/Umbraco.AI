import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

export const testModalManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "UmbracoAI.Modal.Test.CreateOptions",
        name: "Test Create Options Modal",
        element: () => import("./create-options/test-create-options-modal.element.js"),
    },
    {
        type: "modal",
        alias: "Uai.Modal.GraderConfigEditor",
        name: "Grader Config Editor Modal",
        element: () => import("./grader-config-editor/grader-config-editor-modal.element.js"),
    },
];
