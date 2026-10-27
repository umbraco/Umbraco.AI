/**
 * The kind of value an Extract Values field pulls out of the content.
 */
export type UaiExtractionFieldKind = "text" | "number" | "date" | "time" | "datetime" | "duration" | "list";

/** The unit of a duration field. */
export type UaiExtractionDurationUnit = "seconds" | "minutes" | "hours" | "days" | "weeks" | "months" | "years";

/** The type of a list field's items. */
export type UaiExtractionListItemType = "text" | "number" | "date";

/**
 * One field in a `Uai.PropertyEditorUi.ExtractionFieldList` value. Flat, not polymorphic — fields
 * irrelevant to `kind` are simply omitted, mirroring the server's `ExtractValuesField
 * { Kind, Alias, Instructions, Decimals, Integer, Unit, ItemType }`.
 */
export interface UaiExtractionFieldListItem {
    kind: UaiExtractionFieldKind;
    /** Unique within the list; the field's output key downstream. */
    alias: string;
    /** What to extract, in natural language. */
    instructions: string;
    /** Number and duration only. Decimal places to round to, 0 to 6. */
    decimals?: number;
    /** Number and duration only. Whether the value must be a whole number. Default false. */
    integer?: boolean;
    /** Number: free text (e.g. "DKK"). Duration: a `UaiExtractionDurationUnit`. */
    unit?: string;
    /** List only. The type of each item. Default "text". */
    itemType?: UaiExtractionListItemType;
}
