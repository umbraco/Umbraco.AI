/**
 * The one place the usage dashboard picks a time zone. The chart lays out hours in the browser's zone, so a
 * missing or unknown name means the browser's zone, and UTC only if that can't be resolved. The overview
 * response carries no zone, so callers pass the one they asked with.
 */
export const USAGE_FALLBACK_TIME_ZONE = "UTC";

function validZone(timeZone?: string | null): string | undefined {
    if (!timeZone) {
        return undefined;
    }

    try {
        new Intl.DateTimeFormat("en-US", { timeZone });
        return timeZone;
    } catch {
        return undefined;
    }
}

/** The zone to use for a name that may be missing or not a real IANA zone. */
export function resolveUsageTimeZone(timeZone?: string | null): string {
    return (
        validZone(timeZone) ?? validZone(Intl.DateTimeFormat().resolvedOptions().timeZone) ?? USAGE_FALLBACK_TIME_ZONE
    );
}

/** The local ISO date (`YYYY-MM-DD`) an instant falls on in the zone. */
export function usageLocalDate(instant: Date, timeZone?: string | null): string {
    const parts = new Intl.DateTimeFormat("en-US", {
        timeZone: resolveUsageTimeZone(timeZone),
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
    }).formatToParts(instant);
    const part = (type: Intl.DateTimeFormatPartTypes) => parts.find((p) => p.type === type)?.value ?? "";

    return `${part("year")}-${part("month")}-${part("day")}`;
}
