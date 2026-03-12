import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";


const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.ProfilePicker",
    name: "AI Profile Picker Property Editor UI",
    element: () => import("./property-editor-ui-profile-picker.element.js"),
    meta: {
        label: "AI Profile Picker",
        icon: "icon-wand",
        group: "Umbraco AI",
        settings: {
            properties: [
                {
                    alias: "multiple",
                    label: "Allow Multiple",
                    description: "Allow selecting multiple AI profiles",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.Toggle",
                },
                {
                    alias: "min",
                    label: "Minimum Items",
                    description: "Minimum number of profiles required (optional)",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.Integer",
                },
                {
                    alias: "max",
                    label: "Maximum Items",
                    description: "Maximum number of profiles allowed (optional)",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.Integer",
                },
            ],
        },
    },
};

export const profilePickerPropertyEditorManifests = [propertyEditorUi];
