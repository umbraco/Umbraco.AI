import { UAI_GUARDRAIL_COLLECTION_ALIAS } from "./constants.js";
import { UAI_GUARDRAIL_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";
import { guardrailCollectionActionManifests } from "./action/manifests.js";
import { guardrailBulkActionManifests } from "./bulk-action/manifests.js";

export const guardrailCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_GUARDRAIL_COLLECTION_ALIAS,
        name: "Guardrail Collection",
        element: () => import("./guardrail-collection.element.js"),
        meta: {
            repositoryAlias: UAI_GUARDRAIL_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAI.CollectionView.Guardrail.Table",
        name: "Guardrail Table View",
        element: () => import("./views/table/guardrail-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_GUARDRAIL_COLLECTION_ALIAS }],
    },
    ...guardrailCollectionActionManifests,
    ...guardrailBulkActionManifests,
];
