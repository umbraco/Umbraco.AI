import type { ManifestModal } from "@umbraco-cms/backoffice/modal";
import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.ExtractionFieldList",
    name: "AI Extraction Field List Property Editor UI",
    element: () => import("./property-editor-ui-extraction-field-list.element.js"),
    meta: {
        label: "AI Extraction Field List",
        icon: "icon-filter",
        group: "Umbraco AI",
        keywords: ["ai", "umbraco ai", "extraction", "extract", "fields", "automate"],
        settings: {
            properties: [
                {
                    alias: "max",
                    label: "Maximum Fields",
                    description: "Maximum number of fields allowed (optional, default 20).",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.Integer",
                },
            ],
        },
    },
};

const configModal: ManifestModal = {
    type: "modal",
    alias: "Uai.Modal.ExtractionFieldConfigEditor",
    name: "Extraction Field Config Editor Modal",
    element: () => import("./extraction-field-config-modal.element.js"),
};

export const extractionFieldListPropertyEditorManifests = [propertyEditorUi, configModal];
