import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { NodeTypeDefinition } from "../../editor/rete/rete-node-definitions.js";

export interface UaiOrchestrationNodeTypePickerModalData {
    nodeTypes: NodeTypeDefinition[];
}

export interface UaiOrchestrationNodeTypePickerModalValue {
    selectedType: string;
}

export const UAI_ORCHESTRATION_NODE_TYPE_PICKER_MODAL = new UmbModalToken<
    UaiOrchestrationNodeTypePickerModalData,
    UaiOrchestrationNodeTypePickerModalValue
>("Uai.Modal.OrchestrationNodeTypePicker", {
    modal: { type: "sidebar", size: "small" },
});
