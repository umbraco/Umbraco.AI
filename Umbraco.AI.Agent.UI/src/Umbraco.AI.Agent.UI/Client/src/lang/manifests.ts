import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";

const localizationManifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    {
        type: "localization",
        alias: "UAIAgent.UI.Localization.En",
        weight: -100,
        name: "English",
        meta: {
            culture: "en",
        },
        js: () => import("./en.js"),
    },
];

export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [...localizationManifests];
