import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.MockEntity",
    name: "AI Mock Entity Property Editor UI",
    element: () => import("./property-editor-ui-mock-entity.element.js"),
    meta: {
        label: "AI Mock Entity",
        icon: "icon-document",
        group: "Umbraco AI",
        keywords: ["ai", "umbraco ai", "mock", "fake", "stub", "test", "fixture", "entity"],
    },
};

export const mockEntityPropertyEditorManifests = [propertyEditorUi];
