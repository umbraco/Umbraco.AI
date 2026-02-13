import { sectionMenuManifests } from "./menu/manifests.js";
import { sectionSidebarManifests } from "./sidebar/manifests.js";
import { UAI_SECTION_ALIAS } from "./constants.ts";

const section: UmbExtensionManifest = {
    type: "section",
    alias: UAI_SECTION_ALIAS,
    name: "AI Section",
    meta: {
        label: "AI",
        pathname: "ai",
    },
    conditions: [
        {
            alias: 'Umb.Condition.SectionUserPermission',
            match: UAI_SECTION_ALIAS,
        },
    ],
};

export const sectionManifests = [
    ...sectionMenuManifests,
    ...sectionSidebarManifests,
    section
];
