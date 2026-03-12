import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.EntityPropertyPicker",
    name: "AI Entity Property Picker Property Editor UI",
    element: () => import("./property-editor-ui-entity-property-picker.element.js"),
    meta: {
        label: "AI Entity Property Picker",
        icon: "icon-list",
        group: "Umbraco AI",
        settings: {
            properties: [
                {
                    alias: "entityTypeField",
                    label: "Entity Type Field",
                    description: "The alias of the sibling field that contains the entity type",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
                {
                    alias: "entityIdField",
                    label: "Entity ID Field",
                    description: "The alias of the sibling field that contains the entity ID",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
            ],
        },
    },
};

export const entityPropertyPickerPropertyEditorManifests = [propertyEditorUi];
