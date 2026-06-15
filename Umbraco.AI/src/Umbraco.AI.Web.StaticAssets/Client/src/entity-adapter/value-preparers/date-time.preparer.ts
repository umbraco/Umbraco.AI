import type { UaiPropertyValuePreparerApi } from "./types.js";

/**
 * Preparer for the date / datetime / time editor (`Umbraco.DateTime`).
 *
 * The CMS date picker stores values in a timezone-naive `yyyy-MM-dd HH:mm:ss` form (space, not
 * `T`) and its display logic splits on a literal space to render the input — see
 * `property-editor-ui-date-picker.element.ts` `#formatValue` in the CMS. LLMs almost always emit
 * ISO 8601 (`2026-05-08T14:30:00`, sometimes with `Z` or a timezone offset, sometimes with
 * milliseconds), and date-only strings (`2026-05-08`) are common too. setPropertyValue accepts
 * those without throwing, but the input renders blank because the format check fails silently.
 *
 * Normalisation rules:
 * - Trim and pass through anything that already has the canonical space form.
 * - Replace the date/time separator `T` with a space.
 * - Strip a trailing `Z` or numeric timezone offset (`+HH:mm` / `-HH:mm`) — the editor is
 *   timezone-naive, so the safest thing is to keep the wall-clock components.
 * - Strip fractional seconds (`.123`) — the editor only handles second precision.
 * - Pad date-only input to midnight so the split-on-space invariant holds.
 *
 * Anything that doesn't look like a date (objects, numbers, etc.) is returned unchanged — the
 * editor will reject it downstream and surface its own error rather than us guessing.
 */
export default class DateTimePreparer implements UaiPropertyValuePreparerApi {
    prepare(value: unknown): unknown {
        if (typeof value !== "string") {
            return value;
        }

        const trimmed = value.trim();
        if (trimmed === "") {
            return trimmed;
        }

        // Drop any timezone suffix (Z or ±HH:mm / ±HHmm). The editor is timezone-naive and
        // re-attaching the offset would just confuse the format check downstream.
        let normalised = trimmed.replace(/(Z|[+-]\d{2}:?\d{2})$/i, "");

        // Drop fractional seconds — the editor renders second-precision only.
        normalised = normalised.replace(/\.\d+$/, "");

        // Canonicalise the date/time separator.
        normalised = normalised.replace("T", " ");

        // Pad date-only input out to midnight so the editor's `split(' ')` check finds a time.
        if (!normalised.includes(" ") && /^\d{4}-\d{2}-\d{2}$/.test(normalised)) {
            normalised = `${normalised} 00:00:00`;
        }

        return normalised;
    }
}
