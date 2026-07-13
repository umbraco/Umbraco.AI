import type { ManifestPropertyAction } from "@umbraco-cms/backoffice/property-action";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";
import { UAI_PROMPT_PROPERTY_ACTION_PREFIX, UAI_PROMPT_SCOPE_CONDITION_ALIAS } from "./constants.js";
import type { UaiPromptRegistrationModel, UaiPromptPropertyActionMeta } from "./types.js";
import type { UaiPromptScopeConditionConfig } from "./prompt-scope.condition.js";

/**
 * Gets the property editor UIs that a prompt should appear on based on its scope.
 * This is used for initial filtering - detailed filtering by content type and property alias
 * is handled by the scope condition.
 */
function getPropertyEditorUisForScope(prompt: UaiPromptRegistrationModel): string[] {
    // If no scope, prompt is not allowed anywhere
    if (!prompt.scope) {
        return [];
    }

    // If no allow rules, prompt is not allowed anywhere
    if (prompt.scope.allowRules.length === 0) {
        return [];
    }

    // Collect all property editor UI aliases from allow rules
    const editorAliases = new Set<string>();
    for (const rule of prompt.scope.allowRules) {
        if (rule.propertyEditorUiAliases && rule.propertyEditorUiAliases.length > 0) {
            rule.propertyEditorUiAliases.forEach((alias) => editorAliases.add(alias));
        }
    }

    // If no specific editors defined, the scope applies to every registered property editor UI
    // (filtered further by content type or property alias via the condition). Generation is
    // constrained per-editor via its value schema (see IPropertyEditorSchemaService), falling
    // back to plain-string generation for editors that don't expose one.
    if (editorAliases.size === 0) {
        return umbExtensionsRegistry
            .getByType<"propertyEditorUi", ManifestPropertyEditorUi>("propertyEditorUi")
            .map((manifest) => manifest.alias);
    }

    return Array.from(editorAliases);
}

/**
 * Generates a property action manifest for a prompt.
 * The manifest registers a property action that will appear based on scope configuration.
 *
 * Uses a combination of:
 * - forPropertyEditorUis: Initial filtering by property editor type
 * - conditions: Full scope filtering including content type and property alias
 */
export function generatePromptPropertyActionManifest(
    prompt: UaiPromptRegistrationModel,
    weight: number = 100,
): ManifestPropertyAction<UaiPromptPropertyActionMeta> | null {
    // TipTapTool prompts are registered as toolbar extensions, not property actions
    if (prompt.displayMode === "TipTapTool") {
        return null;
    }

    const propertyEditorUis = getPropertyEditorUisForScope(prompt);

    // If no property editors to show on, don't create a manifest
    if (propertyEditorUis.length === 0) {
        return null;
    }

    return {
        type: "propertyAction",
        kind: "default",
        alias: `${UAI_PROMPT_PROPERTY_ACTION_PREFIX}.${prompt.alias}`,
        name: `Insert Prompt: ${prompt.name}`,
        forPropertyEditorUis: propertyEditorUis,
        api: () => import("./prompt-insert.property-action.js"),
        weight,
        meta: {
            icon: "icon-wand",
            label: prompt.name,
            promptUnique: prompt.unique,
            promptDescription: prompt.description ?? null,
            promptScope: prompt.scope ?? null,
            uiMode: prompt.uiMode ?? "modal",
        },
        conditions: [
            {
                alias: UAI_PROMPT_SCOPE_CONDITION_ALIAS,
                scope: prompt.scope ?? null,
            } as UaiPromptScopeConditionConfig,
        ],
    };
}
