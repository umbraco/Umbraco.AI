/**
 * Value Preparation Utilities
 *
 * Shared logic for preparing values before applying them to workspace properties.
 * Used by both document and block adapters to handle editor-specific value formats.
 */

/**
 * Editors whose values are pre-built object envelopes when supplied by the backend property
 * value operation dispatcher. These must NOT be re-processed by `prepareValueForEditor` — the
 * dispatcher returns the canonical envelope shape already, and feeding it through the
 * `JSON.parse` / RichText-wrap branches below silently corrupts it.
 */
const PRE_BUILT_ENVELOPE_EDITORS = new Set([
    "Umbraco.BlockList",
    "Umbraco.BlockGrid",
    "Umbraco.RichText",
]);

/**
 * Prepare a value for setting on a property, handling editor-specific formats.
 *
 * @param value - The raw value to prepare
 * @param editorAlias - The property editor alias (e.g., "Umbraco.RichText", "Umbraco.TextBox")
 * @param currentValue - The current value of the property (for preserving structure)
 * @returns The prepared value ready to be set on the property
 */
export function prepareValueForEditor(value: unknown, editorAlias?: string, currentValue?: unknown): unknown {
    // Pre-built envelope guard: when the backend dispatcher returns a structured value for a
    // block-shaped editor, accept it verbatim. The `JSON.parse` branch below would either throw
    // (caught and ignored, leaving the object intact, then incorrectly re-stringified by the
    // RichText branch) or pass through (with a key-rewrite for MediaPicker3 that mutates an
    // already-correct envelope). Either path is a silent corruption — short-circuit instead.
    if (
        value !== null &&
        typeof value === "object" &&
        editorAlias !== undefined &&
        PRE_BUILT_ENVELOPE_EDITORS.has(editorAlias)
    ) {
        return value;
    }

    let valueToSet: unknown = value;

    // Try to parse JSON values
    try {
        valueToSet = JSON.parse(valueToSet as string);
        if (editorAlias === "Umbraco.MediaPicker3") {
            (valueToSet as Array<{ key: string }>)[0].key = uuidv4();
        }
    } catch {
        // Not JSON, use as-is
    }

    // Wrap in TipTap's expected format for RichText properties
    if (editorAlias === "Umbraco.RichText") {
        const current = currentValue as { markup?: string; blocks?: object } | undefined;
        valueToSet = {
            markup: typeof valueToSet === "string" ? valueToSet : String(valueToSet),
            blocks: current?.blocks ?? { layout: {}, contentData: [], settingsData: [], expose: [] },
        };
    }

    return valueToSet;
}

function uuidv4(): string {
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, (c) =>
        (+c ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (+c / 4)))).toString(16),
    );
}
