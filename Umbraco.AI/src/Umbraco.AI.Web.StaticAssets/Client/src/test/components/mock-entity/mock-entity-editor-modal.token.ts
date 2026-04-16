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

/**
 * Submit value of the editor modal. Includes `| undefined` so the modal can be
 * registered via UmbModalRouteRegistrationController (whose onSetup return type
 * requires `value` only when the token's value type is non-undefined). The modal
 * itself only ever reaches `submit()` after building a real value, so consumers
 * only need to handle undefined on the route-setup callback path.
 */
export type UaiMockEntityEditorModalValue =
    | {
          mockEntityJson: string;
      }
    | undefined;

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
