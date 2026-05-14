import type { UaiPropertyValuePreparerApi } from "./types.js";

/**
 * Preparer for the media-picker-3 editor.
 *
 * The picker's thumbnail subcomponent fetches the resolved media on mount and doesn't re-fetch
 * when only `mediaKey` changes within the same picker entry. Lit re-mounts a subcomponent when
 * its surrounding entry's `key` changes, so we re-mint the entry's `key` (distinct from
 * `mediaKey`) whenever its non-key content actually changed against the staged value.
 *
 * - matched + unchanged content: keep `key` (no churn for entries the LLM didn't touch — focal
 *   points, crops, and other per-entry settings stay stable).
 * - matched + content differs: re-mint `key` (forces re-mount, refreshes the thumbnail).
 * - no match: keep `key` (genuinely new entry, e.g. handler-minted fresh by `add_item`; the LLM
 *   may reference it later via `remove_item({ blockKey })` so we must not change it).
 */
export default class MediaPicker3Preparer implements UaiPropertyValuePreparerApi {
    prepare(value: unknown, currentValue: unknown): unknown {
        let valueToSet: unknown = value;
        try {
            valueToSet = JSON.parse(valueToSet as string);
        } catch {
            // Not JSON, use as-is
        }

        if (!Array.isArray(valueToSet)) {
            return valueToSet;
        }

        const oldByKey = new Map<string, Record<string, unknown>>();
        if (Array.isArray(currentValue)) {
            for (const entry of currentValue as Array<Record<string, unknown>>) {
                const k = entry?.key;
                if (typeof k === "string") {
                    oldByKey.set(k, entry);
                }
            }
        }

        return (valueToSet as Array<Record<string, unknown>>).map((entry) => {
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
}

function omitKey(entry: Record<string, unknown>): Record<string, unknown> {
    const { key: _key, ...rest } = entry;
    return rest;
}
