import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiImageGenerationServerDataSource } from "./image-generation.server.data-source.js";
import type { UaiImageGenerationRequest, UaiImageGenerationResult } from "../types.js";

/**
 * Repository for image-generation operations.
 */
export class UaiImageGenerationRepository extends UmbControllerBase {
    #dataSource: UaiImageGenerationServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiImageGenerationServerDataSource(host);
    }

    /**
     * Generates one or more images from a text prompt.
     */
    async generate(request: UaiImageGenerationRequest): Promise<{ data?: UaiImageGenerationResult; error?: unknown }> {
        return this.#dataSource.generate(request);
    }
}
