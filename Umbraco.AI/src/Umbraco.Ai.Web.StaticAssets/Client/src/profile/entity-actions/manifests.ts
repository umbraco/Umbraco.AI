import { UAI_PROFILE_ENTITY_TYPE, UAI_PROFILE_ROOT_ENTITY_TYPE } from "../constants.js";

export const profileEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAi.EntityAction.Profile.Create",
        name: "Create Profile Entity Action",
        weight: 1200,
        api: () => import("./profile-create.action.js"),
        forEntityTypes: [UAI_PROFILE_ROOT_ENTITY_TYPE],
        meta: {
            icon: "icon-add",
            label: "Create",
            additionalOptions: true,
        },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAi.EntityAction.Profile.Delete",
        name: "Delete Profile Entity Action",
        weight: 100,
        api: () => import("./profile-delete.action.js"),
        forEntityTypes: [UAI_PROFILE_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
    },
];
