import { UAI_PROFILE_COLLECTION_ALIAS } from "../../constants.js";

export const profileCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAI.CollectionAction.Profile.Create",
        name: "Create Profile",
        element: () => import("./profile-create-collection-action.element.js"),
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_PROFILE_COLLECTION_ALIAS }],
    },
];
