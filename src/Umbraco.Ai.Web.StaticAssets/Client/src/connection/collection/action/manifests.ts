import type { ManifestCollectionAction } from "@umbraco-cms/backoffice/collection";
import { UAI_CONNECTION_ENTITY_TYPE, UAI_CONNECTION_COLLECTION_ALIAS } from "../../constants.js";

export const connectionCollectionActionManifests: ManifestCollectionAction[] = [
    {
        type: "collectionAction",
        kind: "button",
        alias: "UmbracoAi.CollectionAction.Connection.Create",
        name: "Create Connection",
        meta: {
            label: "Create",
            href: `section/settings/workspace/${UAI_CONNECTION_ENTITY_TYPE}/create`,
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONNECTION_COLLECTION_ALIAS }],
    },
];
