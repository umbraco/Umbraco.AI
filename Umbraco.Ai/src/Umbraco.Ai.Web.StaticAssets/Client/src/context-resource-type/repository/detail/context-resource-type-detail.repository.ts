import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiContextResourceTypeDetailServerDataSource } from "./context-resource-type-detail.server.data-source.js";
import type { UaiContextResourceTypeDetailModel } from "../../types.js";

/**
 * Repository for fetching contextResourceType details.
 * Provides full contextResourceType information including setting definitions.
 */
export class UaiContextResourceTypeDetailRepository extends UmbControllerBase {
    #dataSource: UaiContextResourceTypeDetailServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiContextResourceTypeDetailServerDataSource(host);
    }

    /**
     * Requests full contextResourceType details by ID.
     */
    async requestById(id: string): Promise<{ data?: UaiContextResourceTypeDetailModel; error?: unknown }> {
        return this.#dataSource.get(id);
    }
}
