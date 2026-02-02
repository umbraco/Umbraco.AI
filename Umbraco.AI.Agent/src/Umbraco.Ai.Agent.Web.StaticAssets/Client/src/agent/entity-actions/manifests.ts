import { UAI_AGENT_ENTITY_TYPE, UAI_AGENT_ROOT_ENTITY_TYPE } from "../constants.js";

export const agentEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAiAgent.EntityAction.Agent.Create",
        name: "Create Agent Entity Action",
        weight: 1200,
        api: () => import("./agent-create.action.js"),
        forEntityTypes: [UAI_AGENT_ROOT_ENTITY_TYPE],
        meta: {
            icon: "icon-add",
            label: "Create",
        },
    },
    {
        type: "entityAction",
        kind: "delete",
        alias: "UmbracoAiAgent.EntityAction.Agent.Delete",
        name: "Delete Agent Entity Action",
        weight: 100,
        api: () => import("./agent-delete.action.js"),
        forEntityTypes: [UAI_AGENT_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
    },
];
