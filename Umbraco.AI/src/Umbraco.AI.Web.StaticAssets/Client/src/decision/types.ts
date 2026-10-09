/**
 * A yes/no question, answered with a probability.
 * @public
 */
export interface UaiBinaryDecisionQuestion {
    kind: "binary";
    instructions: string;
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
    options: UaiDecisionOption[];
}

/**
 * One level of a {@link UaiScoreDecisionQuestion}'s scale.
 * @public
 */
export interface UaiDecisionScoreLevel {
    description: string;
}

/**
 * A question scored against an ordered list of levels (lowest first).
 * @public
 */
export interface UaiScoreDecisionQuestion {
    kind: "score";
    instructions: string;
    levels: UaiDecisionScoreLevel[];
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
    trueProbability: number;
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
    probabilities: Record<string, number>;
    confidence?: number | null;
    modelId?: string | null;
    usage?: UaiDecisionUsage | null;
}

/**
 * Result of a {@link UaiScoreDecisionQuestion}. `probabilities` is keyed by level index
 * (`0` is the lowest level), not by the level's description.
 * @public
 */
export interface UaiScoreDecisionResult {
    kind: "score";
    score: number;
    probabilities: Record<number, number>;
    confidence?: number | null;
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
    /** The content being judged. Optional; omitted means the question stands alone. */
    state?: string;
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
    state?: string;
    profileIdOrAlias?: string | null;
    signal?: AbortSignal;
}
