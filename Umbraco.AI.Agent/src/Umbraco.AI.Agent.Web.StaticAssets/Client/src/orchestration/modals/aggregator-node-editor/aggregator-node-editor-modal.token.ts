import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationNode } from "../../types.js";

export interface UaiOrchestrationAggregatorNodeEditorModalData {
    node: UaiOrchestrationNode;
}

export interface UaiOrchestrationAggregatorNodeEditorModalValue {
    node: UaiOrchestrationNode;
    deleted?: boolean;
}

export const UAI_ORCHESTRATION_AGGREGATOR_NODE_EDITOR_MODAL = new UmbModalToken<
    UaiOrchestrationAggregatorNodeEditorModalData,
    UaiOrchestrationAggregatorNodeEditorModalValue
>("Uai.Modal.OrchestrationAggregatorNodeEditor", {
    modal: { type: "sidebar", size: "medium" },
});
