import { UaiNoOpCollectionAction } from "@umbraco-ai/core";
import { UAI_AGENT_COLLECTION_ALIAS } from "../constants.js";

export const agentCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAIAgent.CollectionAction.Agent.Create",
        name: "Create Agent",
        element: () => import("./agent-create-collection-action.element.js"),
        api: UaiNoOpCollectionAction,
        meta: { label: "Create Agent" },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_AGENT_COLLECTION_ALIAS }],
    },
];
