import type { ManifestUaiRequestContextContributor } from "./extension-type.js";

export const requestContextManifests: ManifestUaiRequestContextContributor[] = [
	{
		type: "uaiRequestContextContributor",
		alias: "UmbracoAI.RequestContextContributor.Section",
		name: "Section Request Context Contributor",
		api: () => import("./contributors/section.contributor.js"),
		weight: 100,
	},
	{
		type: "uaiRequestContextContributor",
		alias: "UmbracoAI.RequestContextContributor.Entity",
		name: "Entity Request Context Contributor",
		api: () => import("./contributors/entity.contributor.js"),
		weight: 200,
	},
];
