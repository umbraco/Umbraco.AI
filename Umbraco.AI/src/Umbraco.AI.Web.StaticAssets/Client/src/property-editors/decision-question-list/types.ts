import type { UaiKeyValueListItem } from "../key-value-list/property-editor-ui-key-value-list.element.js";

/**
 * The kind of question asked of a Decision-capable AI profile.
 */
export type UaiDecisionQuestionKind = "binary" | "choice" | "score";

/**
 * One question in a `Uai.PropertyEditorUi.DecisionQuestionList` value. Flat, not polymorphic
 * (ARCHITECTURE decision 6) — fields irrelevant to `kind` are simply omitted, mirroring the
 * server's `AskDecisionsQuestion { Kind, Alias, Instructions, TrueCriteria, FalseCriteria,
 * Threshold, Options[{Key,Value}], Levels[] }`.
 */
export interface UaiDecisionQuestionListItem {
    kind: UaiDecisionQuestionKind;
    /** Unique within the list; the question's output key downstream. */
    alias: string;
    instructions: string;
    /** Binary only. What counts as "yes", beyond the instructions. */
    trueCriteria?: string;
    /** Binary only. What counts as "no", beyond the instructions. */
    falseCriteria?: string;
    /** Binary only. Minimum probability counted as "yes". Default 0.5. */
    threshold?: number;
    /** Choice only. 2..255 entries with unique, non-blank keys. */
    options?: UaiKeyValueListItem[];
    /** Score only. 2..10 non-blank labels, lowest first. */
    levels?: string[];
}
