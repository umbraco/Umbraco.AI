import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiExtractionServerDataSource } from "./extraction.server.data-source.js";
import type { UaiExtractionResult, UaiExtractionServerRequest } from "../types.js";

/**
 * Repository for extraction operations.
 */
export class UaiExtractionRepository extends UmbControllerBase {
    #dataSource: UaiExtractionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiExtractionServerDataSource(host);
    }

    /**
     * Extracts typed values from content using an Extraction-capable AI profile.
     */
    async extract(request: UaiExtractionServerRequest): Promise<{ data?: UaiExtractionResult; error?: unknown }> {
        return this.#dataSource.extract(request);
    }
}
