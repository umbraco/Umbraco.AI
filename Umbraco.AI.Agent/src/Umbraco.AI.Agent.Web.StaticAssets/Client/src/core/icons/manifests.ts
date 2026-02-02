export const iconManifests: Array<UmbExtensionManifest> = [
	{
		type: 'icons',
		alias: 'UmbracoAiAgent.Icons',
		name: 'Umbraco AI Agent Icons',
		js: () => import('./icons.js'),
	},
];
