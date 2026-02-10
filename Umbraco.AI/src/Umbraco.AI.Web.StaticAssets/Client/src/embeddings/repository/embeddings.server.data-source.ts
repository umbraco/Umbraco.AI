import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { EmbeddingsService } from "../../api/sdk.gen.js";
import type { UaiEmbeddingRequest, UaiEmbeddingResult } from "../types.js";

/**
 * Server data source for embedding operations.
 */
export class UaiEmbeddingsServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Generates embeddings for the provided values.
     */
    async generate(request: UaiEmbeddingRequest): Promise<{ data?: UaiEmbeddingResult; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            EmbeddingsService.generateEmbeddings({
                body: {
                    profileIdOrAlias: request.profileId ?? null,
                    values: request.values,
                },
                signal: request.signal,
            }),
        );

        if (error || !data) {
            return { error };
        }

        return {
            data: {
                embeddings: data.embeddings.map((e) => ({
                    index: e.index,
                    vector: e.vector,
                })),
            },
        };
    }
}
