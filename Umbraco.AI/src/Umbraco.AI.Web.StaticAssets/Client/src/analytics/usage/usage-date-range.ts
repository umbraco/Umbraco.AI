import { uauiPresetRange } from "@umbraco/analytics-ui";
import type { UauiCompare, UauiDatePreset, UauiDateRange } from "@umbraco/analytics-ui";
import { usageLocalDate } from "./adapter/usage-time-zone.js";

/** The date range and optional comparison the Usage dashboard shows. */
export interface UaiUsageDateRange {
    range: UauiDateRange;
    compare?: UauiCompare;
}

/** The last 7 days, counted back from today in the browser's zone, with no comparison. */
export function defaultUsageDateRange(): UaiUsageDateRange {
    return {
        range: { ...uauiPresetRange("last-7-days", usageLocalDate(new Date()))!, preset: "last-7-days" },
    };
}

/** Today and Yesterday are opt-in presets: they are what makes the hourly bars reachable. */
export const USAGE_DATE_PRESETS: UauiDatePreset[] = [
    { id: "today" },
    { id: "yesterday" },
    { id: "last-7-days" },
    { id: "last-30-days" },
    { id: "this-month" },
    { id: "last-month" },
    { id: "last-year" },
];

export function isSameUsageDateRange(a: UaiUsageDateRange, b: UaiUsageDateRange): boolean {
    return (
        a.range.from === b.range.from &&
        a.range.to === b.range.to &&
        a.compare?.from === b.compare?.from &&
        a.compare?.to === b.compare?.to
    );
}
