export const iconManifests: Array<UmbExtensionManifest> = [
	{
		type: "icons",
		alias: "UmbracoAI.Icons",
		name: "Umbraco AI Icons",
		js: () => import("./icons.js"),
	},
];
