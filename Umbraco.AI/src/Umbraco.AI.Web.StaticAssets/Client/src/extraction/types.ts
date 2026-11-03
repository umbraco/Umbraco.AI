/**
 * A character range within the `state` text an answer was read from. `start` is inclusive,
 * `end` is exclusive.
 * @public
 */
export interface UaiExtractionSpan {
    start: number;
    end: number;
}

/**
 * Extracts a piece of text.
 * @public
 */
export interface UaiTextExtractionField {
    kind: "text";
    id: string;
    instructions: string;
}

/**
 * Extracts a number.
 * @public
 */
export interface UaiNumberExtractionField {
    kind: "number";
    id: string;
    instructions: string;
    /** Number of decimal places to keep. */
    decimals?: number;
    /** Whether the value must be a whole number. Defaults to `false`. */
    integer?: boolean;
    /** The unit the value is expressed in (e.g. `DKK`). */
    unit?: string;
}

/**
 * Extracts a calendar date (ISO 8601 `yyyy-MM-dd`).
 * @public
 */
export interface UaiDateExtractionField {
    kind: "date";
    id: string;
    instructions: string;
}

/**
 * Extracts a time of day.
 * @public
 */
export interface UaiTimeExtractionField {
    kind: "time";
    id: string;
    instructions: string;
}

/**
 * Extracts a date and time.
 * @public
 */
export interface UaiDateTimeExtractionField {
    kind: "datetime";
    id: string;
    instructions: string;
}

/**
 * Units a duration field can be expressed in.
 * @public
 */
export type UaiExtractionDurationUnit = "seconds" | "minutes" | "hours" | "days" | "weeks" | "months" | "years";

/**
 * Item kinds a list field can hold.
 * @public
 */
export type UaiExtractionListItemType = "text" | "number" | "date";

/**
 * Extracts a length of time, expressed in `unit`.
 * @public
 */
export interface UaiDurationExtractionField {
    kind: "duration";
    id: string;
    instructions: string;
    /** The unit the value is expressed in. */
    unit: UaiExtractionDurationUnit;
    /** Number of decimal places to keep. */
    decimals?: number;
    /** Whether the value must be a whole number. Defaults to `false`. */
    integer?: boolean;
}

/**
 * Extracts a list of values.
 * @public
 */
export interface UaiListExtractionField {
    kind: "list";
    id: string;
    instructions: string;
    /** The kind of each item in the list. */
    itemType?: UaiExtractionListItemType;
}

/**
 * A value to extract. The `kind` discriminates the shape of both the field and its matching
 * answer.
 * @public
 */
export type UaiExtractionField =
    | UaiTextExtractionField
    | UaiNumberExtractionField
    | UaiDateExtractionField
    | UaiTimeExtractionField
    | UaiDateTimeExtractionField
    | UaiDurationExtractionField
    | UaiListExtractionField;

/**
 * Token usage for an extraction request.
 * @public
 */
export interface UaiExtractionUsage {
    inputTokens?: number | null;
    outputTokens?: number | null;
    totalTokens?: number | null;
}

/**
 * Properties shared by every answer. `isFound` is `false` when the value isn't in the content.
 * @public
 */
export interface UaiExtractionAnswerBase {
    /** The model's confidence in the value, between 0 and 1. */
    confidence?: number | null;
    /** The probability that the value is absent from the content. */
    nullProbability?: number | null;
    isFound: boolean;
}

/**
 * Answer to a {@link UaiTextExtractionField}.
 * @public
 */
export interface UaiTextExtractionAnswer extends UaiExtractionAnswerBase {
    kind: "text";
    value?: string | null;
    span?: UaiExtractionSpan | null;
}

/**
 * Answer to a {@link UaiNumberExtractionField}.
 * @public
 */
export interface UaiNumberExtractionAnswer extends UaiExtractionAnswerBase {
    kind: "number";
    value?: number | null;
}

/**
 * Answer to a {@link UaiDateExtractionField}.
 * @public
 */
export interface UaiDateExtractionAnswer extends UaiExtractionAnswerBase {
    kind: "date";
    value?: string | null;
}

/**
 * Answer to a {@link UaiTimeExtractionField}.
 * @public
 */
export interface UaiTimeExtractionAnswer extends UaiExtractionAnswerBase {
    kind: "time";
    value?: string | null;
}

/**
 * Answer to a {@link UaiDateTimeExtractionField}.
 * @public
 */
export interface UaiDateTimeExtractionAnswer extends UaiExtractionAnswerBase {
    kind: "datetime";
    value?: string | null;
}

/**
 * Answer to a {@link UaiDurationExtractionField}.
 * @public
 */
export interface UaiDurationExtractionAnswer extends UaiExtractionAnswerBase {
    kind: "duration";
    value?: number | null;
    unit: string;
}

/**
 * Answer to a {@link UaiListExtractionField}. `spans` is in the same order as `values`.
 * @public
 */
export interface UaiListExtractionAnswer extends UaiExtractionAnswerBase {
    kind: "list";
    values: string[];
    spans: UaiExtractionSpan[];
}

/**
 * One extracted answer. Which shape comes back is determined by the field's `kind`.
 * @public
 */
export type UaiExtractionAnswer =
    | UaiTextExtractionAnswer
    | UaiNumberExtractionAnswer
    | UaiDateExtractionAnswer
    | UaiTimeExtractionAnswer
    | UaiDateTimeExtractionAnswer
    | UaiDurationExtractionAnswer
    | UaiListExtractionAnswer;

/**
 * An extraction request (public API): the content to read and the fields to pull out of it.
 * @public
 */
export interface UaiExtractionRequest {
    /** The content to extract from. */
    state: string;
    /** The date relative expressions like "yesterday" resolve against (ISO 8601 `yyyy-MM-dd`). */
    referenceDate?: string;
    /** The fields to extract. Each `id` must be unique. */
    fields: UaiExtractionField[];
}

/**
 * Result of an extraction request. `answers` is keyed by field `id`, exactly as sent.
 * @public
 */
export interface UaiExtractionResult {
    answers: Record<string, UaiExtractionAnswer>;
    modelId?: string | null;
    usage?: UaiExtractionUsage | null;
}

/**
 * Options for an extraction request (public API).
 * @public
 */
export interface UaiExtractionOptions {
    /** Profile ID (GUID) or alias. If omitted, uses the default Extraction profile. */
    profileIdOrAlias?: string;
    /** AbortSignal for cancellation. */
    signal?: AbortSignal;
}

/**
 * Internal request model for repository/data source.
 * @internal
 */
export interface UaiExtractionServerRequest extends UaiExtractionRequest {
    profileIdOrAlias?: string | null;
    signal?: AbortSignal;
}
