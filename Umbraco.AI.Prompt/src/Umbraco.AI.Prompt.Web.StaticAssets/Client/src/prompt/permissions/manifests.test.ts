// GP-1 (AC1), GP-5, GP-12 (AC1, AC2, AC6)
import { describe, expect, it } from "vitest";
import {
    UAI_AREA_GATED_MANIFEST_TYPES,
    UAI_PERMISSION_ENTITY_TYPE,
    UAI_PERMISSION_GROUP_ADD_ONS,
    UAI_PERMISSION_WEIGHT_ADD_ON_START,
    UAI_PERMISSION_WEIGHT_CORE_START,
} from "@umbraco-ai/core";
import en from "../../lang/en.js";
import { promptManifests } from "../manifests.js";
import { UAI_PERMISSION_VERB_MANAGE_PROMPTS } from "./constants.js";
import { promptPermissionManifests } from "./manifests.js";

type AnyManifest = {
    type: string;
    alias: string;
    weight?: number;
    forEntityTypes?: string[];
    meta?: { verbs?: string[]; label?: string; description?: string; group?: string };
    conditions?: Array<{ alias: string; allOf?: string[] }>;
};

const FALLBACK = "Umb.Condition.UserPermission.Fallback";
const all = promptManifests as AnyManifest[];
const permission = promptPermissionManifests[0] as AnyManifest;
const dictionary = en as unknown as Record<string, Record<string, string>>;
const fallbackConditions = (m: AnyManifest) => (m.conditions ?? []).filter((c) => c.alias === FALLBACK);

describe("Feature: the Manage prompts right", () => {
    describe("Scenario: the right is registered on the user group screen", () => {
        it("registers one entityUserPermission manifest", () => {
            expect(promptPermissionManifests.map((m) => m.type)).toEqual(["entityUserPermission"]);
        });

        it("uses the verb the server checks", () => {
            expect(permission.meta?.verbs).toEqual(["Uai.Prompt.Manage"]);
        });

        it("shares the AI box by using the AI permission entity type", () => {
            expect(permission.forEntityTypes).toEqual([UAI_PERMISSION_ENTITY_TYPE]);
        });

        it("lists it under Add-ons", () => {
            expect(permission.meta?.group).toBe(UAI_PERMISSION_GROUP_ADD_ONS);
        });

        it("sorts it with the add-on weights, below the core range", () => {
            expect(permission.weight).toBe(UAI_PERMISSION_WEIGHT_ADD_ON_START - 1);
            expect(permission.weight).toBeLessThan(UAI_PERMISSION_WEIGHT_CORE_START);
        });

        it("has a label and description in en", () => {
            const keys = [permission.meta?.label, permission.meta?.description].map((term) =>
                (term ?? "").replace("#", "").split("_"),
            );
            expect(keys.filter(([section, key]) => !dictionary[section]?.[key])).toEqual([]);
        });

        it("is included in the prompt manifests the bundle registers", () => {
            expect(all).toContain(permission);
        });
    });

    describe("Scenario: the prompt area needs the right", () => {
        const gated = all.filter((m) => UAI_AREA_GATED_MANIFEST_TYPES.has(m.type));
        const open = all.filter((m) => !UAI_AREA_GATED_MANIFEST_TYPES.has(m.type));

        it("gates the menu item, collection, entity actions and workspaces", () => {
            const types = new Set(gated.map((m) => m.type));
            expect(
                ["menuItem", "collection", "collectionAction", "entityBulkAction", "entityAction", "workspace"].filter(
                    (t) => !types.has(t),
                ),
            ).toEqual([]);
        });

        it("conditions every gated manifest on Manage prompts, and on nothing else", () => {
            const wrong = gated
                .filter(
                    (m) =>
                        JSON.stringify(fallbackConditions(m).flatMap((c) => c.allOf ?? [])) !==
                        JSON.stringify([UAI_PERMISSION_VERB_MANAGE_PROMPTS]),
                )
                .map((m) => m.alias);
            expect(wrong).toEqual([]);
        });

        it("leaves modals, repositories and other views open so pickers keep working", () => {
            expect(open.filter((m) => fallbackConditions(m).length > 0).map((m) => m.alias)).toEqual([]);
            expect(open.length).toBeGreaterThan(0);
        });
    });
});
