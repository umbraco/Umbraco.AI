import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationNode } from "../../types.js";

export interface UaiOrchestrationManagerNodeEditorModalData {
    node: UaiOrchestrationNode;
}

export interface UaiOrchestrationManagerNodeEditorModalValue {
    node: UaiOrchestrationNode;
}

export const UAI_ORCHESTRATION_MANAGER_NODE_EDITOR_MODAL = new UmbModalToken<
    UaiOrchestrationManagerNodeEditorModalData,
    UaiOrchestrationManagerNodeEditorModalValue
>("Uai.Modal.OrchestrationManagerNodeEditor", {
    modal: { type: "sidebar", size: "medium" },
});
