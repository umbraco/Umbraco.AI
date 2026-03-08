import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiAgentType } from "../../types.js";

export interface UaiAgentCreateOptionsModalData {
    headline?: string;
}

export interface UaiAgentCreateOptionsModalValue {
    agentType: UaiAgentType;
}

export const UAI_AGENT_CREATE_OPTIONS_MODAL = new UmbModalToken<
    UaiAgentCreateOptionsModalData,
    UaiAgentCreateOptionsModalValue
>("UmbracoAIAgent.Modal.Agent.CreateOptions", {
    modal: {
        type: "dialog",
        size: "small",
    },
});
