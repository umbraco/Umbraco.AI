import type { ManifestModal } from "@umbraco-cms/backoffice/modal";
import type { ManifestTestMockEntityEditor } from "./mock-entity-editor-extension-type.js";
import { UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE } from "./mock-entity-editor-extension-type.js";

export const testEntityContextManifests: Array<ManifestModal | ManifestTestMockEntityEditor> = [
    {
        type: "modal",
        alias: "Uai.Modal.MockEntityEditor",
        name: "Mock Entity Editor Modal",
        element: () => import("./mock-entity-editor-modal.element.js"),
    },
    {
        type: UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE,
        alias: "Uai.TestMockEntityEditor.CmsEntity",
        name: "CMS Mock Entity Editor",
        forEntityTypes: ["document", "media", "member"],
        element: () => import("./cms-mock-entity-editor.element.js"),
    },
];
