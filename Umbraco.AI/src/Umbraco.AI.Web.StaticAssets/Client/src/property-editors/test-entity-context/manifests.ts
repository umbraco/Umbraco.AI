import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.TestEntityContext",
    name: "AI Test Entity Context Property Editor UI",
    element: () => import("./property-editor-ui-test-entity-context.element.js"),
    meta: {
        label: "AI Test Entity Context",
        icon: "icon-document",
        group: "Umbraco AI",
    },
};

export const testEntityContextPropertyEditorManifests = [propertyEditorUi];
