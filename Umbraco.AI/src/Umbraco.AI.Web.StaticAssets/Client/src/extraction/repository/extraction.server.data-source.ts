import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ExtractionService } from "../../api/sdk.gen.js";
// On v17, the generated `ExtractionFieldModel`/`ExtractionAnswerModel` are plain base types with no
// `$type` discriminator, so they can't be narrowed by kind. This uses the concrete per-kind members
// (as carried by `ExtractRequestModel`/`ExtractResponseModel`) instead.
import type { ExtractRequestModel, ExtractResponseModel } from "../../api/types.gen.js";
import type {
    UaiExtractionAnswer,
    UaiExtractionField,
    UaiExtractionResult,
    UaiExtractionServerRequest,
} from "../types.js";

type ExtractionFieldWireModel = ExtractRequestModel["fields"][number];
type ExtractionAnswerWireModel = ExtractResponseModel["answers"][string];

function toFieldModel(field: UaiExtractionField): ExtractionFieldWireModel {
    switch (field.kind) {
        case "text":
        case "date":
        case "time":
        case "datetime":
            return { $type: field.kind, id: field.id, instructions: field.instructions };
        case "number":
            return {
                $type: "number",
                id: field.id,
                instructions: field.instructions,
                decimals: field.decimals ?? undefined,
                integer: field.integer ?? false,
                unit: field.unit ?? undefined,
            };
        case "duration":
            return {
                $type: "duration",
                id: field.id,
                instructions: field.instructions,
                unit: field.unit,
                decimals: field.decimals ?? undefined,
                integer: field.integer ?? false,
            };
        case "list":
            return {
                $type: "list",
                id: field.id,
                instructions: field.instructions,
                itemType: field.itemType ?? undefined,
            };
    }
}

/** Omits confidence/nullProbability entirely when the wire didn't send them, rather than setting `undefined`. */
function toOptionalMetrics(answer: { confidence?: number | null; nullProbability?: number | null }): {
    confidence?: number;
    nullProbability?: number;
} {
    return {
        ...(answer.confidence == null ? {} : { confidence: answer.confidence }),
        ...(answer.nullProbability == null ? {} : { nullProbability: answer.nullProbability }),
    };
}

function toAnswer(answer: ExtractionAnswerWireModel): UaiExtractionAnswer | Error {
    switch (answer.$type) {
        case "text":
            return {
                kind: "text",
                value: answer.value,
                ...(answer.span ? { span: { start: answer.span.start, end: answer.span.end } } : {}),
                ...toOptionalMetrics(answer),
                isFound: answer.isFound,
            };
        case "number":
            return { kind: "number", value: answer.value, ...toOptionalMetrics(answer), isFound: answer.isFound };
        case "date":
            return { kind: "date", value: answer.value, ...toOptionalMetrics(answer), isFound: answer.isFound };
        case "time":
            return { kind: "time", value: answer.value, ...toOptionalMetrics(answer), isFound: answer.isFound };
        case "datetime":
            return { kind: "datetime", value: answer.value, ...toOptionalMetrics(answer), isFound: answer.isFound };
        case "duration":
            return {
                kind: "duration",
                value: answer.value,
                unit: answer.unit,
                ...toOptionalMetrics(answer),
                isFound: answer.isFound,
            };
        case "list":
            return {
                kind: "list",
                values: answer.values,
                spans: answer.spans.map((span) => ({ start: span.start, end: span.end })),
                ...toOptionalMetrics(answer),
                isFound: answer.isFound,
            };
        default: {
            // Exhaustiveness check: adding a new answer $type to the union without a matching
            // case above fails the build here rather than silently dropping data.
            const exhaustiveCheck: never = answer;
            const unrecognized = exhaustiveCheck as { $type?: unknown };
            return new Error(`Unknown extraction answer $type: ${String(unrecognized.$type)}`);
        }
    }
}

function toResult(response: ExtractResponseModel): { data?: UaiExtractionResult; error?: unknown } {
    const answers: Record<string, UaiExtractionAnswer> = {};
    for (const [id, model] of Object.entries(response.answers)) {
        const answer = toAnswer(model);
        if (answer instanceof Error) {
            // An unrecognized $type means the server is newer than this client; surface it
            // rather than silently dropping the answer.
            return { error: answer };
        }
        answers[id] = answer;
    }

    return {
        data: {
            answers,
            modelId: response.modelId ?? undefined,
            usage: response.usage ?? undefined,
        },
    };
}

/**
 * Server data source for extraction operations.
 */
export class UaiExtractionServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Extracts typed values from content using an Extraction-capable AI profile.
     */
    async extract(request: UaiExtractionServerRequest): Promise<{ data?: UaiExtractionResult; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ExtractionService.extract({
                body: {
                    profileIdOrAlias: request.profileIdOrAlias ?? undefined,
                    state: request.state,
                    referenceDate: request.referenceDate ?? undefined,
                    fields: request.fields.map(toFieldModel),
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
