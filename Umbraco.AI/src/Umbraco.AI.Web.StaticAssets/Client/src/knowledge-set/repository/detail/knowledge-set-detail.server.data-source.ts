import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { KnowledgeSetsService } from "../../../api/sdk.gen.js";
import { UaiKnowledgeSetTypeMapper } from "../../type-mapper.js";
import type { UaiKnowledgeSetDetailModel } from "../../types.js";

/**
 * Server data source for fetching knowledge set details, including their items.
 *
 * Read-only — knowledge sets are code-defined and auto-active, so there is no create/update/delete.
 */
export class UaiKnowledgeSetDetailServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches a knowledge set by ID with its full item content.
     */
    async get(id: string): Promise<{ data?: UaiKnowledgeSetDetailModel; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, KnowledgeSetsService.getKnowledgeSetById({ path: { id } }));

        if (error || !data) {
            return { error };
        }

        return { data: UaiKnowledgeSetTypeMapper.toDetailModel(data) };
    }
}
