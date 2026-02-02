import { UAI_PROFILE_COLLECTION_ALIAS } from "./constants.js";
import { UAI_PROFILE_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";
import { profileCollectionActionManifests } from "./action/manifests.js";
import { profileBulkActionManifests } from "./bulk-action/manifests.js";

export const profileCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_PROFILE_COLLECTION_ALIAS,
        name: "Profile Collection",
        element: () => import("./profile-collection.element.js"),
        meta: {
            repositoryAlias: UAI_PROFILE_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAi.CollectionView.Profile.Table",
        name: "Profile Table View",
        element: () => import("./views/table/profile-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_PROFILE_COLLECTION_ALIAS }],
    },
    ...profileCollectionActionManifests,
    ...profileBulkActionManifests,
];
