import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.EntityPicker",
    name: "AI Entity Picker Property Editor UI",
    element: () => import("./property-editor-ui-entity-picker.element.js"),
    meta: {
        label: "AI Entity Picker",
        icon: "icon-document",
        group: "Umbraco AI",
        settings: {
            properties: [
                {
                    alias: "entityTypeField",
                    label: "Entity Type Field",
                    description: "The alias of the sibling field that contains the entity type",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
            ],
        },
    },
};

export const entityPickerPropertyEditorManifests = [propertyEditorUi];
