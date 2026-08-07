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
        keywords: ["ai", "umbraco ai", "masked", "password", "secret", "sensitive", "api key"],
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
