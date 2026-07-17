import { UAI_KNOWLEDGE_SET_COLLECTION_ALIAS } from "./constants.js";
import { UAI_KNOWLEDGE_SET_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";

/**
 * Knowledge Set collection manifests.
 *
 * Read-only, unlike the Context collection: no create action, no bulk-delete action, and no custom
 * collection element — the `default` collection kind's built-in element (with its default toolbar) is
 * used as-is.
 */
export const knowledgeSetCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_KNOWLEDGE_SET_COLLECTION_ALIAS,
        name: "Knowledge Set Collection",
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
