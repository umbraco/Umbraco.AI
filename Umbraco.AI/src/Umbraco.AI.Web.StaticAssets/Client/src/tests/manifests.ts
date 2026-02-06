import { ManifestTypes } from "@umbraco-cms/backoffice/extension-registry";

const sectionAlias = "Umbraco.AI.Section.Tests";

export const manifests: Array<ManifestTypes> = [
    {
        type: "section",
        alias: sectionAlias,
        name: "AI Tests Section",
        weight: 100,
        meta: {
            label: "Tests",
            pathname: "tests",
        },
    },
    {
        type: "sectionView",
        alias: "Umbraco.AI.SectionView.Tests",
        name: "AI Tests Section View",
        element: () => import("./workspace/tests-workspace-root.element.js"),
        weight: 100,
        meta: {
            label: "Tests",
            pathname: "tests",
            icon: "icon-list",
        },
        conditions: [
            {
                alias: "Umb.Condition.SectionAlias",
                match: sectionAlias,
            },
        ],
    },
    {
        type: "menuItem",
        kind: "tree",
        alias: "Umbraco.AI.MenuItem.Tests",
        name: "Tests Menu Item",
        weight: 100,
        meta: {
            label: "Tests",
            menus: ["Umbraco.AI.Menu.SettingsMenu"],
        },
    },
];
