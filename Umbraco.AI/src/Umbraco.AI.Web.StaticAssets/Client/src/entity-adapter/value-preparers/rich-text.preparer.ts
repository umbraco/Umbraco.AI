import type { UaiPropertyValuePreparerApi } from "./types.js";

interface RichTextValue {
    markup: string;
    blocks: object;
}

const EMPTY_BLOCKS = { layout: {}, contentData: [], settingsData: [], expose: [] };

/**
 * Preparer for the rich-text editor.
 *
 * Two flows fold into one preparer:
 * - The dispatcher returns a fully-built `{ markup, blocks }` object envelope; we pass it
 *   through unchanged to avoid re-stringifying.
 * - The LLM may also call `set_value` with a bare markup string. Tiptap's editor expects the
 *   wrapped shape, so we lift the string into `{ markup, blocks }` while preserving the existing
 *   blocks envelope from the workspace's staged value (so we don't drop existing inline blocks).
 */
export default class RichTextPreparer implements UaiPropertyValuePreparerApi {
    prepare(value: unknown, currentValue: unknown): unknown {
        if (value !== null && typeof value === "object") {
            return value;
        }

        let parsed: unknown = value;
        try {
            parsed = JSON.parse(value as string);
        } catch {
            // Not JSON, treat as raw markup
        }

        if (parsed !== null && typeof parsed === "object") {
            return parsed;
        }

        const current = currentValue as Partial<RichTextValue> | undefined;
        return {
            markup: typeof parsed === "string" ? parsed : String(parsed),
            blocks: current?.blocks ?? EMPTY_BLOCKS,
        };
    }
}
