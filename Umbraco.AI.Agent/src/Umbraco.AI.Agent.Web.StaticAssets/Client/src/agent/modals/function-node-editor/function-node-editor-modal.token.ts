import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationNode } from "../../types.js";

export interface UaiOrchestrationFunctionNodeEditorModalData {
    node: UaiOrchestrationNode;
}

export interface UaiOrchestrationFunctionNodeEditorModalValue {
    node: UaiOrchestrationNode;
    deleted?: boolean;
}

export const UAI_ORCHESTRATION_FUNCTION_NODE_EDITOR_MODAL = new UmbModalToken<
    UaiOrchestrationFunctionNodeEditorModalData,
    UaiOrchestrationFunctionNodeEditorModalValue
>("Uai.Modal.OrchestrationFunctionNodeEditor", {
    modal: { type: "sidebar", size: "medium" },
});
