import {
    UMB_ENTITY_ACTION_GROUP_CREATE,
    UMB_ENTITY_ACTION_GROUP_STRUCTURE,
} from "@umbraco-cms/backoffice/entity-action";
import { UAI_CONNECTION_ENTITY_TYPE, UAI_CONNECTION_ROOT_ENTITY_TYPE } from "../constants.js";

export const connectionEntityActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "entityAction",
        kind: "default",
        group: UMB_ENTITY_ACTION_GROUP_CREATE,
        alias: "UmbracoAI.EntityAction.Connection.Create",
        name: "Create Connection Entity Action",
        weight: 1200,
        api: () => import("./connection-create.action.js"),
        forEntityTypes: [UAI_CONNECTION_ROOT_ENTITY_TYPE],
        meta: {
            icon: "icon-add",
            label: "Create",
            additionalOptions: true,
        },
    },
    {
        type: "entityAction",
        kind: "default",
        group: UMB_ENTITY_ACTION_GROUP_STRUCTURE,
        alias: "UmbracoAI.EntityAction.Connection.Delete",
        name: "Delete Connection Entity Action",
        weight: 100,
        api: () => import("./connection-delete.action.js"),
        forEntityTypes: [UAI_CONNECTION_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
    },
];
