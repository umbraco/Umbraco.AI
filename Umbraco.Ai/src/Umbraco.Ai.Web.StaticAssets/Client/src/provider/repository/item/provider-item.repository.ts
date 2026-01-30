import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiProviderItemServerDataSource } from "./provider-item.server.data-source.js";
import type { UaiProviderItemModel } from "../../types.js";

/**
 * Repository for fetching provider items.
 * Simple repository without store - providers are read-only and rarely change.
 */
export class UaiProviderItemRepository extends UmbControllerBase {
    #dataSource: UaiProviderItemServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiProviderItemServerDataSource(host);
    }

    /**
     * Requests all available providers.
     */
    async requestItems(): Promise<{ data?: UaiProviderItemModel[]; error?: unknown }> {
        return this.#dataSource.getItems();
    }
}
