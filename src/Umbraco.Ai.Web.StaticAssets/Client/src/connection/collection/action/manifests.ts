import type { ManifestCollectionAction } from "@umbraco-cms/backoffice/collection";
import { UaiConnectionConstants } from "../../constants.js";

export const connectionCollectionActionManifests: ManifestCollectionAction[] = [
    {
        type: "collectionAction",
        kind: "button",
        alias: "UmbracoAi.CollectionAction.Connection.Create",
        name: "Create Connection",
        meta: {
            label: "Create",
            href: `section/settings/workspace/${UaiConnectionConstants.EntityType.Entity}/create`,
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UaiConnectionConstants.Collection }],
    },
];
