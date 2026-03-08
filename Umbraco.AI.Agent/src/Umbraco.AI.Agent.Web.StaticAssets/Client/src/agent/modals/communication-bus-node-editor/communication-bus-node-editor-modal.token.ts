import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationNode } from "../../types.js";

export interface UaiOrchestrationCommunicationBusNodeEditorModalData {
	node: UaiOrchestrationNode;
}

export interface UaiOrchestrationCommunicationBusNodeEditorModalValue {
	node: UaiOrchestrationNode;
	deleted?: boolean;
}

export const UAI_ORCHESTRATION_COMMUNICATION_BUS_NODE_EDITOR_MODAL = new UmbModalToken<
	UaiOrchestrationCommunicationBusNodeEditorModalData,
	UaiOrchestrationCommunicationBusNodeEditorModalValue
>("Uai.Modal.OrchestrationCommunicationBusNodeEditor", {
	modal: { type: "sidebar", size: "medium" },
});
