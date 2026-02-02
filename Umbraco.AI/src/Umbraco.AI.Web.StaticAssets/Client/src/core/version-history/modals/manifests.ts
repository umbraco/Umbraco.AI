export const manifests: UmbExtensionManifest[] = [
    {
        type: "modal",
        alias: "Uai.Modal.Rollback",
        name: "Rollback Modal",
        element: () => import("./rollback-modal/rollback-modal.element.js"),
    },
];
