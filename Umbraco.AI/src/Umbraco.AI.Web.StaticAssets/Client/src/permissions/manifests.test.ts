// GP-1 (AC1, AC4)
import { describe, expect, it } from "vitest";
import * as constants from "./constants.js";
import { UAI_CORE_PERMISSIONS, getPermissionLocalizationKey, permissionManifests } from "./manifests.js";
import en from "../lang/en.js";

type PermissionManifest = {
    alias: string;
    forEntityTypes: string[];
    meta: { verbs: string[]; label: string; description: string; group: string };
};

const manifests = permissionManifests as unknown as PermissionManifest[];
const verbConstants = Object.entries(constants)
    .filter(([name]) => name.startsWith("UAI_PERMISSION_VERB_"))
    .map(([, value]) => value as string);
const manifestFor = (verb: string) => manifests.filter((m) => m.meta.verbs.includes(verb));
const dictionary = en as unknown as Record<string, Record<string, string>>;

describe("Feature: AI rights on the user group screen", () => {
    describe("Scenario: the core permission manifests are registered", () => {
        it("registers 9 entityUserPermission manifests", () => {
            expect(permissionManifests.map((m) => m.type)).toEqual(Array(9).fill("entityUserPermission"));
        });

        it("registers every one under the AI permission entity type", () => {
            expect(manifests.map((m) => m.forEntityTypes)).toEqual(
                Array(9).fill([constants.UAI_PERMISSION_ENTITY_TYPE]),
            );
        });

        it("puts Manage connections under Configuration", () => {
            expect(manifestFor(constants.UAI_PERMISSION_VERB_MANAGE_CONNECTIONS)[0].meta.group).toBe(
                constants.UAI_PERMISSION_GROUP_CONFIGURATION,
            );
        });

        it("puts View logs under Monitoring", () => {
            expect(manifestFor(constants.UAI_PERMISSION_VERB_VIEW_AUDIT_LOGS)[0].meta.group).toBe(
                constants.UAI_PERMISSION_GROUP_MONITORING,
            );
        });

        it("gives every manifest its table row's verb and group", () => {
            expect(manifests.map((m) => ({ verb: m.meta.verbs[0], group: m.meta.group }))).toEqual(
                UAI_CORE_PERMISSIONS,
            );
        });

        it("gives every manifest a unique alias", () => {
            expect(new Set(manifests.map((m) => m.alias)).size).toBe(manifests.length);
        });
    });

    describe("Scenario: every verb constant has a manifest", () => {
        it("has 9 verb constants", () => {
            expect(verbConstants).toHaveLength(9);
        });

        it("has exactly one manifest for each verb constant", () => {
            expect(verbConstants.map((verb) => manifestFor(verb).length)).toEqual(Array(verbConstants.length).fill(1));
        });

        it("has no manifest for a verb that has no constant", () => {
            expect(manifests.map((m) => m.meta.verbs[0]).filter((verb) => !verbConstants.includes(verb))).toEqual([]);
        });
    });

    describe("Scenario: the labels render text, not raw keys", () => {
        it("defines a label and description in en for every manifest", () => {
            const missing = manifests.flatMap((m) =>
                [m.meta.label, m.meta.description].filter((ref) => {
                    const [section, key] = ref.replace(/^#/, "").split("_");
                    return !dictionary[section]?.[key];
                }),
            );
            expect(missing).toEqual([]);
        });

        it("derives the label key from the verb", () => {
            expect(getPermissionLocalizationKey("Uai.KnowledgeSet.Manage")).toBe("knowledgeSetManage");
        });

        it("defines a heading in en for every group", () => {
            const missing = manifests.map((m) => m.meta.group).filter((group) => !dictionary.actionCategories?.[group]);
            expect(missing).toEqual([]);
        });

        it("defines a heading for the AI box", () => {
            expect(dictionary.user?.[`permissionsEntityGroup_${constants.UAI_PERMISSION_ENTITY_TYPE}`]).toBe("AI");
        });
    });

    describe("Scenario: only core is installed", () => {
        it("has no Manage agents manifest in core", () => {
            expect(manifestFor("Uai.Agent.Manage")).toEqual([]);
        });
    });
});
