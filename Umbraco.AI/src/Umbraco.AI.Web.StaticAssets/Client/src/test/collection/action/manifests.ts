import { UAI_TEST_COLLECTION_ALIAS } from "../../constants.js";

export const testCollectionActionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collectionAction",
        alias: "UmbracoAI.CollectionAction.Test.Create",
        name: "Create Test",
        element: () => import("./test-create-collection-action.element.js"),
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_TEST_COLLECTION_ALIAS }],
    },
];
