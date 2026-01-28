import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiEmbeddingsServerDataSource } from "./embeddings.server.data-source.js";
import type { UaiEmbeddingRequest, UaiEmbeddingResult } from "../types.js";

/**
 * Repository for embedding operations.
 */
export class UaiEmbeddingsRepository extends UmbControllerBase {
    #dataSource: UaiEmbeddingsServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiEmbeddingsServerDataSource(host);
    }

    /**
     * Generates embeddings for the provided values.
     */
    async generate(request: UaiEmbeddingRequest): Promise<{ data?: UaiEmbeddingResult; error?: unknown }> {
        return this.#dataSource.generate(request);
    }
}
