import { UAI_PROPERTY_ACTION_GROUP_AI } from "./constants.js";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "propertyActionGroup",
        alias: UAI_PROPERTY_ACTION_GROUP_AI,
        name: "AI Property Action Group",
        // Below the CMS Clipboard (1200) and Edit (1100) groups, above ungrouped actions such as Clear (0)
        weight: 500,
        meta: { label: "#uaiActionGroups_ai" },
    },
];
