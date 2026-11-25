import {
    UAI_PERMISSION_ENTITY_TYPE,
    UAI_PERMISSION_GROUP_CONFIGURATION,
    UAI_PERMISSION_GROUP_MONITORING,
    UAI_PERMISSION_VERB_MANAGE_CONNECTIONS,
    UAI_PERMISSION_VERB_MANAGE_CONTEXTS,
    UAI_PERMISSION_VERB_MANAGE_GUARDRAILS,
    UAI_PERMISSION_VERB_MANAGE_KNOWLEDGE_SETS,
    UAI_PERMISSION_VERB_MANAGE_PROFILES,
    UAI_PERMISSION_VERB_MANAGE_SETTINGS,
    UAI_PERMISSION_VERB_MANAGE_TESTS,
    UAI_PERMISSION_VERB_VIEW_ANALYTICS,
    UAI_PERMISSION_VERB_VIEW_AUDIT_LOGS,
    UAI_PERMISSION_WEIGHT_CORE_START,
} from "./constants.js";

/**
 * The one table the core rights are built from, in the order they show on the user group screen.
 * The manifest alias and the `uaiPermissions_*` label and description keys are derived from the verb.
 */
export const UAI_CORE_PERMISSIONS: ReadonlyArray<{ verb: string; group: string }> = [
    { verb: UAI_PERMISSION_VERB_MANAGE_CONNECTIONS, group: UAI_PERMISSION_GROUP_CONFIGURATION },
    { verb: UAI_PERMISSION_VERB_MANAGE_PROFILES, group: UAI_PERMISSION_GROUP_CONFIGURATION },
    { verb: UAI_PERMISSION_VERB_MANAGE_CONTEXTS, group: UAI_PERMISSION_GROUP_CONFIGURATION },
    { verb: UAI_PERMISSION_VERB_MANAGE_KNOWLEDGE_SETS, group: UAI_PERMISSION_GROUP_CONFIGURATION },
    { verb: UAI_PERMISSION_VERB_MANAGE_GUARDRAILS, group: UAI_PERMISSION_GROUP_CONFIGURATION },
    { verb: UAI_PERMISSION_VERB_MANAGE_SETTINGS, group: UAI_PERMISSION_GROUP_CONFIGURATION },
    { verb: UAI_PERMISSION_VERB_VIEW_AUDIT_LOGS, group: UAI_PERMISSION_GROUP_MONITORING },
    { verb: UAI_PERMISSION_VERB_VIEW_ANALYTICS, group: UAI_PERMISSION_GROUP_MONITORING },
    { verb: UAI_PERMISSION_VERB_MANAGE_TESTS, group: UAI_PERMISSION_GROUP_MONITORING },
];

/** The verbs the table assigns to a group, so a group's gate never needs its own list. */
export function verbsInGroup(group: string): string[] {
    return UAI_CORE_PERMISSIONS.filter((permission) => permission.group === group).map(({ verb }) => verb);
}

/** "Uai.Connection.Manage" becomes ["Connection", "Manage"]. */
function verbParts(verb: string): string[] {
    return verb.split(".").slice(1);
}

/** The key stem under `uaiPermissions` for a verb, e.g. "connectionManage". */
export function getPermissionLocalizationKey(verb: string): string {
    const [first, ...rest] = verbParts(verb);
    return first.charAt(0).toLowerCase() + first.slice(1) + rest.join("");
}

export const permissionManifests: Array<UmbExtensionManifest> = UAI_CORE_PERMISSIONS.map(({ verb, group }, index) => {
    const key = getPermissionLocalizationKey(verb);
    return {
        type: "entityUserPermission",
        alias: `Uai.EntityUserPermission.${verbParts(verb).join(".")}`,
        name: `AI Permission ${verbParts(verb).join(" ")}`,
        weight: UAI_PERMISSION_WEIGHT_CORE_START - index,
        forEntityTypes: [UAI_PERMISSION_ENTITY_TYPE],
        meta: {
            verbs: [verb],
            label: `#uaiPermissions_${key}Label`,
            description: `#uaiPermissions_${key}Description`,
            group,
        },
    };
});
