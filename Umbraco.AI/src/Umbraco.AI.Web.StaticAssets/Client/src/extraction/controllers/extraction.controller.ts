import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiExtractionRepository } from "../repository/extraction.repository.js";
import type { UaiExtractionOptions, UaiExtractionRequest, UaiExtractionResult } from "../types.js";

/**
 * Public API for pulling typed values (text, numbers, dates, durations, lists) out of
 * free-form content with an Extraction-capable AI profile.
 *
 * Extraction is experimental and only available when the `Umbraco:AI:Experimental:Extraction`
 * feature flag is enabled server-side; otherwise the server returns 404. A 404 (or a 400 for an
 * invalid request) comes back as `error`, never thrown.
 * @public
 */
export class UaiExtractionController extends UmbControllerBase {
    #repository: UaiExtractionRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiExtractionRepository(host);
    }

    /**
     * Extracts the requested fields from the content.
     * @param request - The content and the fields to extract.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns The answers keyed by field id, or error.
     */
    async extract(
        request: UaiExtractionRequest,
        options?: UaiExtractionOptions,
    ): Promise<{ data?: UaiExtractionResult; error?: unknown }> {
        return this.#repository.extract({
            state: request.state,
            referenceDate: request.referenceDate,
            fields: request.fields,
            profileIdOrAlias: options?.profileIdOrAlias,
            signal: options?.signal,
        });
    }
}
