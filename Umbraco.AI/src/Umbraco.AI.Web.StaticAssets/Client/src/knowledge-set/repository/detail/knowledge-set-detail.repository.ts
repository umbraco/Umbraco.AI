import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiKnowledgeSetDetailServerDataSource } from "./knowledge-set-detail.server.data-source.js";
import type { UaiKnowledgeSetDetailModel } from "../../types.js";

/**
 * Repository for fetching knowledge set details, including their items.
 *
 * Read-only — provides load-by-id only; there is no create/save/delete.
 */
export class UaiKnowledgeSetDetailRepository extends UmbControllerBase {
    #dataSource: UaiKnowledgeSetDetailServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiKnowledgeSetDetailServerDataSource(host);
    }

    /**
     * Requests full knowledge set details by ID.
     */
    async requestById(id: string): Promise<{ data?: UaiKnowledgeSetDetailModel; error?: unknown }> {
        return this.#dataSource.get(id);
    }
}

export { UaiKnowledgeSetDetailRepository as api };
