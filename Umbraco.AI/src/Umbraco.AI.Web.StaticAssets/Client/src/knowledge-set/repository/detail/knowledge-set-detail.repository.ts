import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiKnowledgeSetDetailServerDataSource } from "./knowledge-set-detail.server.data-source.js";
import type { UaiKnowledgeSetDetailModel, UaiKnowledgeSetItemContentModel } from "../../types.js";

/**
 * Repository for fetching knowledge set details and item content.
 *
 * Read-only — provides load-by-id and per-item content fetch only; there is no create/save/delete.
 */
export class UaiKnowledgeSetDetailRepository extends UmbControllerBase {
    #dataSource: UaiKnowledgeSetDetailServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiKnowledgeSetDetailServerDataSource(host);
    }

    /**
     * Requests knowledge set details (metadata + item metadata) by ID.
     */
    async requestById(id: string): Promise<{ data?: UaiKnowledgeSetDetailModel; error?: unknown }> {
        return this.#dataSource.get(id);
    }

    /**
     * Requests the markdown content for a single item, fetched lazily on demand.
     */
    async requestItemContent(
        id: string,
        key: string,
    ): Promise<{ data?: UaiKnowledgeSetItemContentModel; error?: unknown }> {
        return this.#dataSource.getItemContent(id, key);
    }
}

export { UaiKnowledgeSetDetailRepository as api };
