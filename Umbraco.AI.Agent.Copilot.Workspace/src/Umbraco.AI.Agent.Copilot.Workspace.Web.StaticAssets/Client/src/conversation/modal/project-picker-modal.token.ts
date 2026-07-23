import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export const UAI_PROJECT_PICKER_MODAL_ALIAS = "Uai.CopilotWorkspace.Modal.ProjectPicker";

export interface UaiProjectPickerModalItem {
    id: string;
    name: string;
    description?: string | null;
}

export interface UaiProjectPickerModalData {
    projects: UaiProjectPickerModalItem[];
    /** Optional custom dialog headline (defaults to the "New chat in a project" term). */
    headline?: string;
    /** When set, a leading "no project" row is shown that resolves `{ projectId: null }` (used by Move). */
    noProjectLabel?: string;
}

export interface UaiProjectPickerModalValue {
    /** The chosen project id, or `null` when the "no project" row was picked. */
    projectId: string | null;
}

/**
 * Centered "create-style" picker for choosing a project (mirrors the CMS create modal). Shared by both
 * "New chat in a project" (start a chat) and the Move-to-project entity action (via `noProjectLabel`,
 * which adds a "remove from project" option).
 */
export const UAI_PROJECT_PICKER_MODAL = new UmbModalToken<UaiProjectPickerModalData, UaiProjectPickerModalValue>(
    UAI_PROJECT_PICKER_MODAL_ALIAS,
    { modal: { type: "dialog" } },
);
