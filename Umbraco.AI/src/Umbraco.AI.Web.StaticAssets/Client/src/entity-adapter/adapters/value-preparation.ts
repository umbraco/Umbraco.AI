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
    } catch {
        // Not JSON, use as-is
    }

    // MediaPicker3: re-mint the entry's `key` when its content changed against the staged value.
    // The picker's thumbnail subcomponent fetches the resolved media on mount and doesn't re-fetch
    // when only `mediaKey` changes within the same entry; replacing the entry's `key` forces lit to
    // unmount and re-mount, which re-fetches the thumbnail. We only regen when the entry's
    // non-key content actually changed — that way add_item's handler-minted fresh keys stay stable
    // (so the LLM's subsequent remove_item({blockKey}) still resolves), and unchanged entries
    // don't churn their per-entry settings (focal point, crops, …).
    if (editorAlias === "Umbraco.MediaPicker3" && Array.isArray(valueToSet)) {
        const oldByKey = new Map<string, Record<string, unknown>>();
        if (Array.isArray(currentValue)) {
            for (const entry of currentValue as Array<Record<string, unknown>>) {
                const k = entry?.key;
                if (typeof k === "string") {
                    oldByKey.set(k, entry);
                }
            }
        }

        valueToSet = (valueToSet as Array<Record<string, unknown>>).map((entry) => {
            const key = typeof entry?.key === "string" ? entry.key : undefined;
            if (!key) {
                return { ...entry, key: crypto.randomUUID() };
            }
            const old = oldByKey.get(key);
            if (!old) {
                return entry;
            }
            if (JSON.stringify(omitKey(old)) === JSON.stringify(omitKey(entry))) {
                return entry;
            }
            return { ...entry, key: crypto.randomUUID() };
        });
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

function omitKey(entry: Record<string, unknown>): Record<string, unknown> {
    const { key: _key, ...rest } = entry;
    return rest;
}
