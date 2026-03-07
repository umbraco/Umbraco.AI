import { UAI_ORCHESTRATION_COLLECTION_ALIAS } from "./constants.js";
import { UAI_ORCHESTRATION_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";
import { orchestrationCollectionActionManifests } from "./action/manifests.js";
import { orchestrationBulkActionManifests } from "./bulk-action/manifests.js";

export const orchestrationCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_ORCHESTRATION_COLLECTION_ALIAS,
        name: "Orchestration Collection",
        element: () => import("./orchestration-collection.element.js"),
        meta: {
            repositoryAlias: UAI_ORCHESTRATION_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAIAgent.CollectionView.Orchestration.Table",
        name: "Orchestration Table View",
        element: () => import("./views/table/orchestration-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_ORCHESTRATION_COLLECTION_ALIAS }],
    },
    ...orchestrationCollectionActionManifests,
    ...orchestrationBulkActionManifests,
];
