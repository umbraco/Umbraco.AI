import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationNode } from "../../types.js";

export interface UaiOrchestrationToolCallNodeEditorModalData {
	node: UaiOrchestrationNode;
}

export interface UaiOrchestrationToolCallNodeEditorModalValue {
	node: UaiOrchestrationNode;
	deleted?: boolean;
}

export const UAI_ORCHESTRATION_TOOL_CALL_NODE_EDITOR_MODAL = new UmbModalToken<
	UaiOrchestrationToolCallNodeEditorModalData,
	UaiOrchestrationToolCallNodeEditorModalValue
>("Uai.Modal.OrchestrationToolCallNodeEditor", {
	modal: { type: "sidebar", size: "medium" },
});
