import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import {
    UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
    type ManifestUaiPropertyValuePreparer,
} from "./extension-type.js";

/**
 * Looks up the preparer registered for a given editor schema alias and runs it. Editors with no
 * registered preparer fall back to a permissive default that attempts JSON.parse on string input
 * and returns the result unchanged.
 */
export async function resolveAndPrepareValue(
    value: unknown,
    editorAlias: string | undefined,
    currentValue: unknown,
): Promise<unknown> {
    if (editorAlias) {
        const manifests = umbExtensionsRegistry.getByType(
            UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
        ) as ManifestUaiPropertyValuePreparer[];

        const matched = manifests.find((m) =>
            m.forPropertyEditorSchemaAlias.toLowerCase() === editorAlias.toLowerCase(),
        );

        if (matched) {
            try {
                const module = await matched.api();
                const preparer = new module.default();
                return await preparer.prepare(value, currentValue);
            } catch (e) {
                console.error(
                    `[UaiValuePreparer] Failed to load preparer '${matched.alias}' for editor '${editorAlias}':`,
                    e,
                );
            }
        }
    }

    return defaultPrepare(value);
}

function defaultPrepare(value: unknown): unknown {
    try {
        return JSON.parse(value as string);
    } catch {
        return value;
    }
}
