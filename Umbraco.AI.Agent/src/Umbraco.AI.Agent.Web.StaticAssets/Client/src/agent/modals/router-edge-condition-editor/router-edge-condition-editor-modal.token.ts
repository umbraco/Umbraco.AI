import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationRouteCondition } from "../../types.js";

export interface UaiOrchestrationRouterEdgeConditionEditorModalData {
	edgeId: string;
	condition: UaiOrchestrationRouteCondition | null;
	isDefault: boolean;
	priority: number | null;
	requiresApproval: boolean;
}

export interface UaiOrchestrationRouterEdgeConditionEditorModalValue {
	condition: UaiOrchestrationRouteCondition | null;
	isDefault: boolean;
	priority: number | null;
	requiresApproval: boolean;
}

export const UAI_ORCHESTRATION_ROUTER_EDGE_CONDITION_EDITOR_MODAL = new UmbModalToken<
	UaiOrchestrationRouterEdgeConditionEditorModalData,
	UaiOrchestrationRouterEdgeConditionEditorModalValue
>("Uai.Modal.OrchestrationRouterEdgeConditionEditor", {
	modal: { type: "sidebar", size: "medium" },
});
