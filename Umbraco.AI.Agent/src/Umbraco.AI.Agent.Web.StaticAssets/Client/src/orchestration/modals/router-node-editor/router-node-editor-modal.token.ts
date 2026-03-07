import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationNode } from "../../types.js";

export interface UaiOrchestrationRouterNodeEditorModalData {
    node: UaiOrchestrationNode;
}

export interface UaiOrchestrationRouterNodeEditorModalValue {
    node: UaiOrchestrationNode;
    deleted?: boolean;
}

export const UAI_ORCHESTRATION_ROUTER_NODE_EDITOR_MODAL = new UmbModalToken<
    UaiOrchestrationRouterNodeEditorModalData,
    UaiOrchestrationRouterNodeEditorModalValue
>("Uai.Modal.OrchestrationRouterNodeEditor", {
    modal: { type: "sidebar", size: "medium" },
});
