import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { DecisionService } from "../../api/sdk.gen.js";
// On v17, the generated `DecisionQuestionModel`/`DecisionResponseModel` aliases are plain base
// types with no `$type` discriminator, so they can't be narrowed by kind. This uses the concrete
// Binary/Choice/Score member types (and `AskResponse`) directly instead.
import type {
    AskResponse,
    BinaryDecisionQuestionModel,
    ChoiceDecisionQuestionModel,
    ScoreDecisionQuestionModel,
} from "../../api/types.gen.js";
import type { UaiDecisionQuestion, UaiDecisionRequest, UaiDecisionResult } from "../types.js";

function toQuestionModel(
    question: UaiDecisionQuestion,
): BinaryDecisionQuestionModel | ChoiceDecisionQuestionModel | ScoreDecisionQuestionModel {
    switch (question.kind) {
        case "binary":
            return {
                $type: "binary",
                instructions: question.instructions,
                trueCriteria: question.trueCriteria ?? undefined,
                falseCriteria: question.falseCriteria ?? undefined,
            };
        case "choice":
            return {
                $type: "choice",
                instructions: question.instructions,
                options: question.options,
            };
        case "score":
            return {
                $type: "score",
                instructions: question.instructions,
                levels: question.levels,
            };
    }
}

/**
 * The wire's score probabilities are keyed by level index as strings (`"0"`..`"N-1"`); the
 * public result keys them by number instead. A key that isn't a plain non-negative integer
 * (which shouldn't happen per SPEC, but isn't this mapper's job to validate) is dropped rather
 * than invented as a bogus numeric index.
 */
function toIndexedProbabilities(probabilities: Record<string, number>): Record<number, number> {
    const result: Record<number, number> = {};
    for (const [key, value] of Object.entries(probabilities)) {
        const index = Number(key);
        if (!Number.isInteger(index)) continue;
        result[index] = value;
    }
    return result;
}

/** Omits `confidence` entirely when the wire didn't send one, rather than setting it to `undefined`. */
function toOptionalConfidence(confidence: number | null | undefined): { confidence: number } | Record<string, never> {
    return confidence == null ? {} : { confidence };
}

function toResult(response: AskResponse): { data?: UaiDecisionResult; error?: unknown } {
    switch (response.$type) {
        case "binary":
            return {
                data: {
                    kind: "binary",
                    trueProbability: response.trueProbability,
                    modelId: response.modelId ?? undefined,
                    usage: response.usage ?? undefined,
                },
            };
        case "choice":
            return {
                data: {
                    kind: "choice",
                    choice: response.choice,
                    probabilities: response.probabilities,
                    ...toOptionalConfidence(response.confidence),
                    modelId: response.modelId ?? undefined,
                    usage: response.usage ?? undefined,
                },
            };
        case "score":
            return {
                data: {
                    kind: "score",
                    score: response.score,
                    probabilities: toIndexedProbabilities(response.probabilities),
                    ...toOptionalConfidence(response.confidence),
                    modelId: response.modelId ?? undefined,
                    usage: response.usage ?? undefined,
                },
            };
        default: {
            // Exhaustiveness check: adding a new response $type to the union without a
            // matching case above fails the build here rather than silently dropping data.
            const exhaustiveCheck: never = response;
            const unrecognized = exhaustiveCheck as { $type?: unknown };
            return { error: new Error(`Unknown decision response $type: ${String(unrecognized.$type)}`) };
        }
    }
}

/**
 * Server data source for decision operations.
 */
export class UaiDecisionServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Asks a Decision-capable AI profile a typed question.
     */
    async ask(request: UaiDecisionRequest): Promise<{ data?: UaiDecisionResult; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            DecisionService.ask({
                body: {
                    profileIdOrAlias: request.profileIdOrAlias ?? undefined,
                    state: request.state ?? undefined,
                    question: toQuestionModel(request.question),
                },
                signal: request.signal,
            }),
        );

        if (error || !data) {
            return { error };
        }

        return toResult(data);
    }
}
