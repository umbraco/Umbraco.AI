import type { ManifestCollectionAction } from "@umbraco-cms/backoffice/collection";
import { UAI_CONNECTION_COLLECTION_ALIAS } from "../../constants.js";
import { UAI_CREATE_CONNECTION_WORKSPACE_PATH } from "../../workspace/connection/paths.js";

export const connectionCollectionActionManifests: ManifestCollectionAction[] = [
    {
        type: "collectionAction",
        kind: "button",
        alias: "UmbracoAi.CollectionAction.Connection.Create",
        name: "Create Connection",
        meta: {
            label: "Create",
            href: UAI_CREATE_CONNECTION_WORKSPACE_PATH,
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONNECTION_COLLECTION_ALIAS }],
    },
];
