import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiTestGraderConfig } from "../../types.js";

export interface UaiGraderConfigEditorModalData {
    graderTypeId: string;
    graderTypeName: string;
    existingGrader?: UaiTestGraderConfig;  // If editing
}

export interface UaiGraderConfigEditorModalValue {
    grader: UaiTestGraderConfig;
}

export const UAI_GRADER_CONFIG_EDITOR_MODAL = new UmbModalToken<
    UaiGraderConfigEditorModalData,
    UaiGraderConfigEditorModalValue
>(
    "Uai.Modal.GraderConfigEditor",
    {
        modal: {
            type: "sidebar",
            size: "medium",
        },
    }
);
