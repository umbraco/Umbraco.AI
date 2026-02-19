import { UAI_TEST_ENTITY_TYPE, UAI_TEST_ROOT_ENTITY_TYPE } from "../constants.js";

export const testEntityActionManifests: Array<UmbExtensionManifest> = [
	{
		type: "entityAction",
		kind: "default",
		alias: "UmbracoAI.EntityAction.Test.Create",
		name: "Create Test Entity Action",
		weight: 1200,
		api: () => import("./test-create.action.js"),
		forEntityTypes: [UAI_TEST_ROOT_ENTITY_TYPE],
		meta: {
			icon: "icon-add",
			label: "Create",
			additionalOptions: true,
		},
	},
	{
		type: "entityAction",
		kind: "default",
		alias: "UmbracoAI.EntityAction.Test.Delete",
		name: "Delete Test Entity Action",
		weight: 100,
		api: () => import("./test-delete.action.js"),
		forEntityTypes: [UAI_TEST_ENTITY_TYPE],
		meta: {
			icon: "icon-trash",
			label: "#actions_delete",
		},
	},
];
