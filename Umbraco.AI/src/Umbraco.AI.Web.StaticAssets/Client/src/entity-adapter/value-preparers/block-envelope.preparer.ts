import type { UaiPropertyValuePreparerApi } from "./types.js";

/**
 * Preparer for block-list / block-grid editors.
 *
 * The backend dispatcher returns a fully-built object envelope shaped like
 * `{ layout, contentData, settingsData, expose }`. The legacy preparer path runs `JSON.parse` on
 * any string input and may further mutate the parsed result; both branches silently corrupt
 * a pre-built envelope. We short-circuit object inputs and only fall through to a parse attempt
 * when the value happens to come in as a JSON string.
 */
export default class BlockEnvelopePreparer implements UaiPropertyValuePreparerApi {
    prepare(value: unknown): unknown {
        if (value !== null && typeof value === "object") {
            return value;
        }

        try {
            return JSON.parse(value as string);
        } catch {
            return value;
        }
    }
}
