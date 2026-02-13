import { UAI_SECTION_ALIAS } from "../constants.ts";

const section: UmbExtensionManifest = {
    type: "dashboard",
    alias: 'Uai.Dashboard.AI',
    name: "AI Welcome Dashboard",
    element: () => import('./ai-dashboard.element.js'),
    weight: 10,
    meta: {
        label: 'Welcome',
        pathname: 'welcome',
    },
    conditions: [
        {
            alias: 'Umb.Condition.SectionAlias',
            match: UAI_SECTION_ALIAS,
        },
    ],
};

export const dashboardManifests: UmbExtensionManifest[] = [ section ];
