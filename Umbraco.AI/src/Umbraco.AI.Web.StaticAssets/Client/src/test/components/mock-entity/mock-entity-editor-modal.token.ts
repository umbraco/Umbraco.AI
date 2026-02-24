import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiMockEntityEditorModalData {
    entityType: string;
    subTypeAlias?: string;
    subTypeUnique?: string;
    subTypeName?: string;
    existingValue?: string;
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
