export const manifests: UmbExtensionManifest[] = [
    {
        type: "modal",
        alias: "Uai.Modal.ItemPicker",
        name: "Item Picker Modal",
        element: () => import("./item-picker-modal.element.js"),
    },
];
