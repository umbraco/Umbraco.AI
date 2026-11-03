import type { UaiExtractionFieldKind } from "./types.js";

/**
 * Every field kind, in the order the "Add field" picker lists them. Shared by the list editor
 * (picker, row detail, row icon) and the config modal (headline).
 */
export const EXTRACTION_FIELD_KINDS: ReadonlyArray<{
    kind: UaiExtractionFieldKind;
    labelKey: string;
    defaultLabel: string;
    icon: string;
}> = [
    { kind: "text", labelKey: "uaiExtractionFieldList_kindText", defaultLabel: "Text", icon: "icon-autofill" },
    {
        kind: "number",
        labelKey: "uaiExtractionFieldList_kindNumber",
        defaultLabel: "Number",
        icon: "icon-calculator",
    },
    { kind: "date", labelKey: "uaiExtractionFieldList_kindDate", defaultLabel: "Date", icon: "icon-calendar" },
    { kind: "time", labelKey: "uaiExtractionFieldList_kindTime", defaultLabel: "Time", icon: "icon-time" },
    {
        kind: "datetime",
        labelKey: "uaiExtractionFieldList_kindDatetime",
        defaultLabel: "Date and time",
        icon: "icon-calendar-alt",
    },
    {
        kind: "duration",
        labelKey: "uaiExtractionFieldList_kindDuration",
        defaultLabel: "Duration",
        icon: "icon-timer",
    },
    { kind: "list", labelKey: "uaiExtractionFieldList_kindList", defaultLabel: "List", icon: "icon-bulleted-list" },
];
