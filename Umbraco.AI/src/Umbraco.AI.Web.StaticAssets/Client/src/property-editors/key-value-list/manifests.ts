import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.KeyValueList",
    name: "AI Key/Value List Property Editor UI",
    element: () => import("./property-editor-ui-key-value-list.element.js"),
    meta: {
        label: "AI Key/Value List",
        icon: "icon-list",
        group: "Umbraco AI",
        keywords: ["ai", "umbraco ai", "key value", "list", "options", "pairs"],
        settings: {
            properties: [
                {
                    alias: "keyLabel",
                    label: "Key Label",
                    description: "Label shown above each row's key input (optional).",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
                {
                    alias: "valueLabel",
                    label: "Value Label",
                    description: "Label shown above each row's value input (optional).",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
                {
                    alias: "keyPlaceholder",
                    label: "Key Placeholder",
                    description: "Placeholder text shown in each row's key input (optional).",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
                {
                    alias: "valuePlaceholder",
                    label: "Value Placeholder",
                    description: "Placeholder text shown in each row's value input (optional).",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
                {
                    alias: "min",
                    label: "Minimum Items",
                    description: "Minimum number of rows required (optional).",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.Integer",
                },
                {
                    alias: "max",
                    label: "Maximum Items",
                    description: "Maximum number of rows allowed (optional).",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.Integer",
                },
            ],
        },
    },
};

export const keyValueListPropertyEditorManifests = [propertyEditorUi];
