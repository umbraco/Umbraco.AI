import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiImageGenerationRepository } from "../repository/image-generation.repository.js";
import type { UaiImageGenerationOptions, UaiImageGenerationResult } from "../types.js";

/**
 * Public API for generating images from a text prompt.
 *
 * Image generation is experimental and only available when the
 * `Umbraco:AI:Experimental:ImageGeneration` feature flag is enabled server-side;
 * otherwise the server returns 404.
 * @public
 */
export class UaiImageGenerationController extends UmbControllerBase {
    #repository: UaiImageGenerationRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiImageGenerationRepository(host);
    }

    /**
     * Generates one or more images from a text prompt.
     * @param prompt - The text prompt describing the desired image(s).
     * @param options - Optional configuration (profile ID/alias, count, size, original images, abort signal).
     * @returns The generation result or error.
     */
    async generate(
        prompt: string,
        options?: UaiImageGenerationOptions,
    ): Promise<{ data?: UaiImageGenerationResult; error?: unknown }> {
        return this.#repository.generate({
            prompt,
            profileIdOrAlias: options?.profileIdOrAlias,
            count: options?.count,
            size: options?.size,
            responseFormat: options?.responseFormat,
            originalImages: options?.originalImages,
            signal: options?.signal,
        });
    }
}
