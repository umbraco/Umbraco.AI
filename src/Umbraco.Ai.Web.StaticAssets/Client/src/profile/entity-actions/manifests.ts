import { UAI_PROFILE_ROOT_ENTITY_TYPE } from "../constants.js";

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
];
