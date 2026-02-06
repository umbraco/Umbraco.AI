export const iconManifests: Array<UmbExtensionManifest> = [
    {
        type: "icons",
        alias: "UmbracoAIAgent.Icons",
        name: "Umbraco AI Agent Icons",
        js: () => import("./icons.js"),
    },
];
