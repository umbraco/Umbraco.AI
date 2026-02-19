import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiTestFeatureItemServerDataSource } from "./test-feature-item.server.data-source.js";
import type { UaiTestFeatureItemModel } from "../../types.js";

/**
 * Repository for fetching test feature items.
 * Simple repository without store - test features are read-only and rarely change.
 */
export class UaiTestFeatureItemRepository extends UmbControllerBase {
    #dataSource: UaiTestFeatureItemServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiTestFeatureItemServerDataSource(host);
    }

    /**
     * Requests all available test features.
     */
    async requestItems(): Promise<{ data?: UaiTestFeatureItemModel[]; error?: unknown }> {
        return this.#dataSource.getItems();
    }
}
