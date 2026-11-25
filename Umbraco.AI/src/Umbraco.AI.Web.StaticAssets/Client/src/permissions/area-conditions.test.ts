// GP-5 (AC1, AC3-AC6), GP-4 (AC6)
import { describe, expect, it } from "vitest";
import { analyticsManifests } from "../analytics/manifests.js";
import { auditLogManifests } from "../audit-log/manifests.js";
import { connectionManifests } from "../connection/manifests.js";
import { contextManifests } from "../context/manifests.js";
import { guardrailManifests } from "../guardrail/manifests.js";
import { knowledgeSetManifests } from "../knowledge-set/manifests.js";
import { profileManifests } from "../profile/manifests.js";
import { settingsManifests } from "../settings/manifests.js";
import { testManifests } from "../test/manifests.js";
import { sectionSidebarManifests } from "../section/sidebar/manifests.js";
import {
    UAI_AREA_GATED_MANIFEST_TYPES,
    createAreaPermissionCondition,
    createGroupPermissionCondition,
    createRegisteredGroupPermissionCondition,
} from "./area-conditions.js";
import {
    UAI_PERMISSION_VERB_MANAGE_CONNECTIONS,
    UAI_PERMISSION_VERB_MANAGE_CONTEXTS,
    UAI_PERMISSION_VERB_MANAGE_GUARDRAILS,
    UAI_PERMISSION_VERB_MANAGE_KNOWLEDGE_SETS,
    UAI_PERMISSION_VERB_MANAGE_PROFILES,
    UAI_PERMISSION_VERB_MANAGE_SETTINGS,
    UAI_PERMISSION_VERB_MANAGE_TESTS,
    UAI_PERMISSION_VERB_VIEW_ANALYTICS,
    UAI_PERMISSION_VERB_VIEW_AUDIT_LOGS,
} from "./constants.js";

type AnyManifest = { type: string; alias: string; conditions?: Array<{ alias: string; allOf?: string[] }> };

const FALLBACK = "Umb.Condition.UserPermission.Fallback";
const FULL_CRUD_TYPES = [
    "menuItem",
    "collection",
    "collectionAction",
    "entityBulkAction",
    "entityAction",
    "workspace",
    "workspaceAction",
];

// The types each area must register at least once, so the sweeps below can't pass on an empty list.
// `also` lists manifests that need a second right on top of the area's own (by alias).
const areas: Array<{
    name: string;
    verb: string;
    manifests: AnyManifest[];
    mustHave: string[];
    also?: Record<string, string>;
}> = [
    {
        name: "Connection",
        verb: UAI_PERMISSION_VERB_MANAGE_CONNECTIONS,
        manifests: connectionManifests as AnyManifest[],
        mustHave: FULL_CRUD_TYPES,
    },
    {
        name: "Profile",
        verb: UAI_PERMISSION_VERB_MANAGE_PROFILES,
        manifests: profileManifests as AnyManifest[],
        mustHave: FULL_CRUD_TYPES,
    },
    {
        name: "Context",
        verb: UAI_PERMISSION_VERB_MANAGE_CONTEXTS,
        manifests: contextManifests as AnyManifest[],
        mustHave: FULL_CRUD_TYPES,
    },
    {
        name: "Guardrail",
        verb: UAI_PERMISSION_VERB_MANAGE_GUARDRAILS,
        manifests: guardrailManifests as AnyManifest[],
        mustHave: FULL_CRUD_TYPES,
    },
    {
        name: "Knowledge set",
        verb: UAI_PERMISSION_VERB_MANAGE_KNOWLEDGE_SETS,
        manifests: knowledgeSetManifests as AnyManifest[],
        mustHave: ["menuItem", "collection", "workspace"],
    },
    {
        name: "Settings",
        verb: UAI_PERMISSION_VERB_MANAGE_SETTINGS,
        manifests: settingsManifests as AnyManifest[],
        mustHave: ["menuItem", "workspace", "workspaceAction"],
    },
    {
        name: "Audit log",
        verb: UAI_PERMISSION_VERB_VIEW_AUDIT_LOGS,
        manifests: auditLogManifests as AnyManifest[],
        mustHave: ["menuItem", "collection", "entityBulkAction", "workspace"],
        also: { "UmbracoAI.EntityBulkAction.AuditLog.Delete": UAI_PERMISSION_VERB_MANAGE_SETTINGS },
    },
    {
        name: "Analytics",
        verb: UAI_PERMISSION_VERB_VIEW_ANALYTICS,
        manifests: analyticsManifests as AnyManifest[],
        mustHave: ["menuItem", "workspace"],
    },
    {
        name: "Tests",
        verb: UAI_PERMISSION_VERB_MANAGE_TESTS,
        manifests: testManifests as AnyManifest[],
        mustHave: [
            "menuItem",
            "collection",
            "collectionAction",
            "entityBulkAction",
            "entityAction",
            "workspace",
            "workspaceAction",
        ],
    },
];

const fallbackConditions = (m: AnyManifest) => (m.conditions ?? []).filter((c) => c.alias === FALLBACK);

describe("Feature: areas need their right in the backoffice", () => {
    describe("Scenario: each area's manifests carry its fallback condition", () => {
        for (const { name, verb, manifests, mustHave, also } of areas) {
            describe(name, () => {
                const gated = manifests.filter((m) => UAI_AREA_GATED_MANIFEST_TYPES.has(m.type));
                const open = manifests.filter((m) => !UAI_AREA_GATED_MANIFEST_TYPES.has(m.type));

                it("registers every type that opens or changes the area", () => {
                    const types = new Set(manifests.map((m) => m.type));
                    expect(mustHave.filter((t) => !types.has(t))).toEqual([]);
                });

                it("conditions every gated manifest on its own verb (and no other, bar the listed extras)", () => {
                    const wrong = gated
                        .filter((m) => {
                            const expected = [verb, ...(also?.[m.alias] ? [also[m.alias]] : [])];
                            const found = fallbackConditions(m).flatMap((c) => c.allOf ?? []);
                            return JSON.stringify([...found].sort()) !== JSON.stringify([...expected].sort());
                        })
                        .map((m) => m.alias);
                    expect(wrong).toEqual([]);
                });

                it("leaves modals, repositories, stores and other views unconditioned (pickers keep working)", () => {
                    expect(open.filter((m) => fallbackConditions(m).length > 0).map((m) => m.alias)).toEqual([]);
                    expect(open.length).toBeGreaterThan(0);
                });
            });
        }

        it("keeps the conditions a manifest already had", () => {
            const save = (connectionManifests as AnyManifest[]).find(
                (m) => m.alias === "UmbracoAI.WorkspaceAction.Connection.Save",
            );
            expect(save?.conditions?.map((c) => c.alias)).toEqual(["Umb.Condition.WorkspaceAlias", FALLBACK]);
        });

        it("builds the condition from the verb with allOf", () => {
            expect(createAreaPermissionCondition("Uai.Context.Manage")).toEqual({
                alias: FALLBACK,
                allOf: ["Uai.Context.Manage"],
            });
        });

        it("refuses a group with no verbs, since the CMS would let everyone through an empty oneOf", () => {
            expect(() => createGroupPermissionCondition("No.Such.Group")).toThrow(/no verbs/);
        });
    });
});

describe("Feature: the Add-ons sidebar heading follows the registered add-on rights", () => {
    it("conditions the Add-ons sidebar app on the registered Add-ons group", () => {
        const addOns = (sectionSidebarManifests as unknown as Array<AnyManifest & { conditions: object[] }>).find(
            (m) => m.alias === "UmbracoAI.SectionSidebarApp.AIAddonsMenu",
        );
        expect(addOns?.conditions).toContainEqual(createRegisteredGroupPermissionCondition("uaiAddOns"));
    });
});
