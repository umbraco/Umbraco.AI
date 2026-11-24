import { describe, expect, it } from "vitest";
import { UAI_CORE_PERMISSIONS, verbsInGroup } from "../../permissions/manifests.js";
import { UAI_PERMISSION_GROUP_CONFIGURATION, UAI_PERMISSION_GROUP_MONITORING } from "../../permissions/constants.js";
import { sectionSidebarManifests } from "./manifests.js";

const FALLBACK = "Umb.Condition.UserPermission.Fallback";

function oneOfFor(alias: string): string[] | undefined {
    const manifest = sectionSidebarManifests.find((m) => m.alias === alias);
    const condition = (manifest?.conditions ?? []).find((c) => c.alias === FALLBACK) as
        | { oneOf?: string[] }
        | undefined;
    return condition?.oneOf;
}

describe("Section sidebar permission gates", () => {
    it.each([
        ["UmbracoAI.SectionSidebarApp.AIConfigurationMenu", UAI_PERMISSION_GROUP_CONFIGURATION],
        ["UmbracoAI.SectionSidebarApp.AIMonitoringMenu", UAI_PERMISSION_GROUP_MONITORING],
    ])("%s shows for any right in its group", (alias, group) => {
        const expected = UAI_CORE_PERMISSIONS.filter((p) => p.group === group).map((p) => p.verb);
        expect(expected.length).toBeGreaterThan(0);
        expect(oneOfFor(alias)).toEqual(expected);
    });

    it("verbsInGroup returns nothing for an unknown group", () => {
        expect(verbsInGroup("nope")).toEqual([]);
    });
});
