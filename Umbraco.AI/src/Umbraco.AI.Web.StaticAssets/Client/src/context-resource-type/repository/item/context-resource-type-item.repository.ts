import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiContextResourceTypeItemServerDataSource } from "./context-resource-type-item.server.data-source.js";
import type { UaiContextResourceTypeItemModel } from "../../types.js";

/**
 * Repository for fetching contextResourceType items.
 * Simple repository without store - contextResourceTypes are read-only and rarely change.
 */
export class UaiContextResourceTypeItemRepository extends UmbControllerBase {
    #dataSource: UaiContextResourceTypeItemServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiContextResourceTypeItemServerDataSource(host);
    }

    /**
     * Requests all available contextResourceTypes.
     */
    async requestItems(): Promise<{ data?: UaiContextResourceTypeItemModel[]; error?: unknown }> {
        return this.#dataSource.getItems();
    }
}
