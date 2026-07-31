import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.MaskedTextBox",
    name: "AI Masked Text Box Property Editor UI",
    element: () => import("./property-editor-ui-masked-text-box.element.js"),
    meta: {
        label: "AI Masked Text Box",
        icon: "icon-lock",
        group: "Umbraco AI",
        settings: {
            properties: [
                {
                    alias: "placeholder",
                    label: "Placeholder",
                    description: "Text shown while the field is empty",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
            ],
        },
    },
};

export const maskedTextBoxPropertyEditorManifests = [propertyEditorUi];
