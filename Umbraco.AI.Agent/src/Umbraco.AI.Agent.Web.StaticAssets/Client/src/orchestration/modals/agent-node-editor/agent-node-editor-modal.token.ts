import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationNode } from "../../types.js";

export interface UaiOrchestrationAgentNodeEditorModalData {
    node: UaiOrchestrationNode;
}

export interface UaiOrchestrationAgentNodeEditorModalValue {
    node: UaiOrchestrationNode;
}

export const UAI_ORCHESTRATION_AGENT_NODE_EDITOR_MODAL = new UmbModalToken<
    UaiOrchestrationAgentNodeEditorModalData,
    UaiOrchestrationAgentNodeEditorModalValue
>("Uai.Modal.OrchestrationAgentNodeEditor", {
    modal: { type: "sidebar", size: "medium" },
});
