import { UAI_CONTEXT_ENTITY_TYPE, UAI_CONTEXT_ROOT_ENTITY_TYPE } from "../constants.js";

export const contextEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAi.EntityAction.Context.Create",
        name: "Create Context Entity Action",
        weight: 1200,
        api: () => import("./context-create.action.js"),
        forEntityTypes: [UAI_CONTEXT_ROOT_ENTITY_TYPE],
        meta: {
            icon: "icon-add",
            label: "Create",
        },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "UmbracoAi.EntityAction.Context.Delete",
        name: "Delete Context Entity Action",
        weight: 100,
        api: () => import("./context-delete.action.js"),
        forEntityTypes: [UAI_CONTEXT_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
    },
];
