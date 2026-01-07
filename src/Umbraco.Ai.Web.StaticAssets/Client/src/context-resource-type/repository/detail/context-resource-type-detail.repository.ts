import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiProviderDetailServerDataSource } from "./provider-detail.server.data-source.js";
import type { UaiProviderDetailModel } from "../../types.js";

/**
 * Repository for fetching provider details.
 * Provides full provider information including setting definitions.
 */
export class UaiProviderDetailRepository extends UmbControllerBase {
    #dataSource: UaiProviderDetailServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiProviderDetailServerDataSource(host);
    }

    /**
     * Requests full provider details by ID.
     */
    async requestById(id: string): Promise<{ data?: UaiProviderDetailModel; error?: unknown }> {
        return this.#dataSource.get(id);
    }
}
