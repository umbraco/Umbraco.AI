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
                context: question.context ?? undefined,
                trueCriteria: question.trueCriteria ?? undefined,
                falseCriteria: question.falseCriteria ?? undefined,
            };
        case "choice":
            return {
                $type: "choice",
                instructions: question.instructions,
                context: question.context ?? undefined,
                options: question.options,
            };
        case "score":
            return {
                $type: "score",
                instructions: question.instructions,
                context: question.context ?? undefined,
                levels: question.levels,
            };
    }
}

function toResult(response: AskResponse): { data?: UaiDecisionResult; error?: unknown } {
    switch (response.$type) {
        case "binary":
            return {
                data: {
                    kind: "binary",
                    answer: response.answer,
                    probability: response.probability,
                    confidence: response.confidence,
                    modelId: response.modelId ?? undefined,
                    usage: response.usage ?? undefined,
                },
            };
        case "choice":
            return {
                data: {
                    kind: "choice",
                    choice: response.choice,
                    confidence: response.confidence,
                    probabilities: response.probabilities,
                    modelId: response.modelId ?? undefined,
                    usage: response.usage ?? undefined,
                },
            };
        case "score":
            return {
                data: {
                    kind: "score",
                    score: response.score,
                    level: response.level,
                    confidence: response.confidence,
                    probabilities: response.probabilities,
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
