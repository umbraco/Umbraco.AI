export function formatDateTime(input: string | Date, locale: string = "en-US"): string {
    const date = typeof input === "string" ? new Date(input) : input;

    if (isNaN(date.getTime())) {
        throw new Error("Invalid date input");
    }

    const formatter = new Intl.DateTimeFormat(locale, {
        month: "long",
        day: "numeric",
        year: "numeric",
        hour: "numeric",
        minute: "2-digit",
        second: "2-digit",
        hour12: true,
    });

    const parts = formatter.formatToParts(date);
    const map: Partial<Record<Intl.DateTimeFormatPartTypes, string>> = {};

    for (const part of parts) {
        map[part.type] = part.value;
    }

    return `${map.month} ${map.day}, ${map.year} at ${map.hour}:${map.minute}:${map.second} ${map.dayPeriod}`;
}
