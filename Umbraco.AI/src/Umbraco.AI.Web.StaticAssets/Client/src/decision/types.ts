/**
 * A yes/no question, answered with a probability and confidence.
 * @public
 */
export interface UaiBinaryDecisionQuestion {
    kind: "binary";
    instructions: string;
    context?: string;
    trueCriteria?: string;
    falseCriteria?: string;
}

/**
 * One selectable option for a {@link UaiChoiceDecisionQuestion}.
 * @public
 */
export interface UaiDecisionOption {
    key: string;
    description?: string;
}

/**
 * A pick-one-of-N question, answered with the chosen option's key.
 * @public
 */
export interface UaiChoiceDecisionQuestion {
    kind: "choice";
    instructions: string;
    context?: string;
    options: UaiDecisionOption[];
}

/**
 * A question scored against an ordered list of levels (lowest first).
 * @public
 */
export interface UaiScoreDecisionQuestion {
    kind: "score";
    instructions: string;
    context?: string;
    levels: string[];
}

/**
 * A question to ask a Decision-capable AI profile. The `kind` discriminates the shape of
 * both the question and its matching result.
 * @public
 */
export type UaiDecisionQuestion = UaiBinaryDecisionQuestion | UaiChoiceDecisionQuestion | UaiScoreDecisionQuestion;

/**
 * Token usage for a decision request.
 * @public
 */
export interface UaiDecisionUsage {
    inputTokens?: number | null;
    outputTokens?: number | null;
    totalTokens?: number | null;
}

/**
 * Result of a {@link UaiBinaryDecisionQuestion}.
 * @public
 */
export interface UaiBinaryDecisionResult {
    kind: "binary";
    answer: boolean;
    probability: number;
    confidence: number;
    modelId?: string | null;
    usage?: UaiDecisionUsage | null;
}

/**
 * Result of a {@link UaiChoiceDecisionQuestion}.
 * @public
 */
export interface UaiChoiceDecisionResult {
    kind: "choice";
    choice: string;
    confidence: number;
    probabilities: Record<string, number>;
    modelId?: string | null;
    usage?: UaiDecisionUsage | null;
}

/**
 * Result of a {@link UaiScoreDecisionQuestion}.
 * @public
 */
export interface UaiScoreDecisionResult {
    kind: "score";
    score: number;
    level: string;
    confidence: number;
    probabilities: Record<string, number>;
    modelId?: string | null;
    usage?: UaiDecisionUsage | null;
}

/**
 * Result of a decision request. Which shape comes back is determined by the question's
 * `kind`, not by inspecting this type alone.
 * @public
 */
export type UaiDecisionResult = UaiBinaryDecisionResult | UaiChoiceDecisionResult | UaiScoreDecisionResult;

/**
 * Options for a decision request (public API).
 * @public
 */
export interface UaiDecisionOptions {
    /** Profile ID (GUID) or alias. If omitted, uses the default Decision profile. */
    profileIdOrAlias?: string;
    /** AbortSignal for cancellation. */
    signal?: AbortSignal;
}

/**
 * Internal request model for repository/data source.
 * @internal
 */
export interface UaiDecisionRequest {
    question: UaiDecisionQuestion;
    profileIdOrAlias?: string | null;
    signal?: AbortSignal;
}
