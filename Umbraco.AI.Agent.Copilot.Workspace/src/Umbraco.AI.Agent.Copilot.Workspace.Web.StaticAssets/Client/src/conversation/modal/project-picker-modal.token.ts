import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export const UAI_PROJECT_PICKER_MODAL_ALIAS = "Uai.CopilotWorkspace.Modal.ProjectPicker";

export interface UaiProjectPickerModalItem {
    id: string;
    name: string;
    description?: string | null;
}

export interface UaiProjectPickerModalData {
    projects: UaiProjectPickerModalItem[];
}

export interface UaiProjectPickerModalValue {
    projectId: string;
}

/**
 * Centered "create-style" picker for choosing which project to start a chat in (mirrors the CMS create
 * modal). Distinct from the side `UMB_ITEM_PICKER_MODAL` used for move-to-project, which stays a flyout.
 */
export const UAI_PROJECT_PICKER_MODAL = new UmbModalToken<UaiProjectPickerModalData, UaiProjectPickerModalValue>(
    UAI_PROJECT_PICKER_MODAL_ALIAS,
    { modal: { type: "dialog" } },
);
