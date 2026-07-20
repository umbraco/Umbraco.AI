import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { KnowledgeSetsService } from "../../../api/sdk.gen.js";
import { UaiKnowledgeSetTypeMapper } from "../../type-mapper.js";
import type { UaiKnowledgeSetDetailModel, UaiKnowledgeSetItemContentModel } from "../../types.js";

/**
 * Server data source for fetching knowledge set details and item content.
 *
 * Read-only — knowledge sets are code-defined and auto-active, so there is no create/update/delete.
 */
export class UaiKnowledgeSetDetailServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches a knowledge set by ID with its item metadata (no content).
     */
    async get(id: string): Promise<{ data?: UaiKnowledgeSetDetailModel; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, KnowledgeSetsService.getKnowledgeSetById({ path: { id } }));

        if (error || !data) {
            return { error };
        }

        return { data: UaiKnowledgeSetTypeMapper.toDetailModel(data) };
    }

    /**
     * Fetches the markdown content for a single item, lazily — content is materialised only when this
     * is called (e.g. when the admin opens the item modal), matching the async item model.
     */
    async getItemContent(
        id: string,
        key: string,
    ): Promise<{ data?: UaiKnowledgeSetItemContentModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            KnowledgeSetsService.getKnowledgeSetItemContent({ path: { id, key } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: UaiKnowledgeSetTypeMapper.toItemContentModel(data) };
    }
}
