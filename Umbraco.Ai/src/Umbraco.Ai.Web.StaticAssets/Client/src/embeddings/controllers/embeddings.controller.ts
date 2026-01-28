import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiEmbeddingsRepository } from "../repository/embeddings.repository.js";
import type { UaiEmbeddingOptions, UaiEmbeddingResult } from "../types.js";

/**
 * Public API for generating embeddings.
 * @public
 */
export class UaiEmbeddingsController extends UmbControllerBase {
    #repository: UaiEmbeddingsRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiEmbeddingsRepository(host);
    }

    /**
     * Generates embeddings for a single value.
     * @param value - The text to generate an embedding for.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns The embedding vector or error.
     */
    async generate(
        value: string,
        options?: UaiEmbeddingOptions
    ): Promise<{ data?: number[]; error?: unknown }> {
        const result = await this.#repository.generate({
            profileId: options?.profile,
            values: [value],
            signal: options?.signal
        });

        if (result.error || !result.data) {
            return { error: result.error };
        }

        return { data: result.data.embeddings[0]?.vector };
    }

    /**
     * Generates embeddings for multiple values.
     * @param values - The texts to generate embeddings for.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns The embedding result or error.
     */
    async generateMany(
        values: string[],
        options?: UaiEmbeddingOptions
    ): Promise<{ data?: UaiEmbeddingResult; error?: unknown }> {
        return this.#repository.generate({
            profileId: options?.profile,
            values,
            signal: options?.signal
        });
    }
}
