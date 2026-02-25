import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { ManifestTestMockEntityEditor } from "./mock-entity-editor-extension-type.js";

export interface UaiMockEntityEditorModalData {
    entityType: string;
    subTypeAlias?: string;
    subTypeUnique?: string;
    subTypeName?: string;
    existingValue?: string;
    /** Pre-resolved editor manifest. When omitted the JSON fallback editor is used. */
    editorManifest?: ManifestTestMockEntityEditor;
}

export interface UaiMockEntityEditorModalValue {
    mockEntityJson: string;
}

export const UAI_MOCK_ENTITY_EDITOR_MODAL = new UmbModalToken<
    UaiMockEntityEditorModalData,
    UaiMockEntityEditorModalValue
>(
    "Uai.Modal.MockEntityEditor",
    {
        modal: {
            type: "sidebar",
            size: "large",
        },
    }
);
