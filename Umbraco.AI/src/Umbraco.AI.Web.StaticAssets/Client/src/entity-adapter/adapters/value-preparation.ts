/**
 * Value Preparation Utilities
 *
 * Shared logic for preparing values before applying them to workspace properties.
 * Used by both document and block adapters to handle editor-specific value formats.
 */

/**
 * Prepare a value for setting on a property, handling editor-specific formats.
 *
 * @param value - The raw value to prepare
 * @param editorAlias - The property editor alias (e.g., "Umbraco.RichText", "Umbraco.TextBox")
 * @param currentValue - The current value of the property (for preserving structure)
 * @returns The prepared value ready to be set on the property
 */
export function prepareValueForEditor(value: unknown, editorAlias?: string, currentValue?: unknown): unknown {
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
