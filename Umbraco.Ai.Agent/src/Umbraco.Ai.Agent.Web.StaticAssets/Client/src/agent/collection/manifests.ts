import { UAI_AGENT_COLLECTION_ALIAS } from "./constants.js";
import { UAI_AGENT_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";
import { agentCollectionActionManifests } from "./action/manifests.js";
import { agentBulkActionManifests } from "./bulk-action/manifests.js";

export const agentCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_AGENT_COLLECTION_ALIAS,
        name: "Agent Collection",
        element: () => import("./agent-collection.element.js"),
        meta: {
            repositoryAlias: UAI_AGENT_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAiAgent.CollectionView.Agent.Table",
        name: "Agent Table View",
        element: () => import("./views/table/agent-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_AGENT_COLLECTION_ALIAS }],
    },
    ...agentCollectionActionManifests,
    ...agentBulkActionManifests,
];
