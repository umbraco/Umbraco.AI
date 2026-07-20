import { UAI_KNOWLEDGE_SET_COLLECTION_ALIAS } from "./constants.js";
import { UAI_KNOWLEDGE_SET_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";

/**
 * Knowledge Set collection manifests.
 *
 * Read-only, unlike the Context collection: no create action and no bulk-delete action. It does use a
 * custom collection element (mirroring the Context collection) to render a search field in the toolbar.
 */
export const knowledgeSetCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_KNOWLEDGE_SET_COLLECTION_ALIAS,
        name: "Knowledge Set Collection",
        element: () => import("./knowledge-set-collection.element.js"),
        meta: {
            repositoryAlias: UAI_KNOWLEDGE_SET_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAI.CollectionView.KnowledgeSet.Table",
        name: "Knowledge Set Table View",
        element: () => import("./views/table/knowledge-set-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_KNOWLEDGE_SET_COLLECTION_ALIAS }],
    },
];
