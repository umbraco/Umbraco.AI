import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiTestGraderItemServerDataSource } from "./test-grader-item.server.data-source.js";
import type { TestGraderInfoModel, TestGraderResponseModel } from "../../../api/types.gen.js";

/**
 * Repository for fetching test grader items.
 */
export class UaiTestGraderItemRepository extends UmbControllerBase {
    #dataSource: UaiTestGraderItemServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiTestGraderItemServerDataSource(host);
    }

    /**
     * Requests all available test graders.
     */
    async requestItems(): Promise<{ data?: TestGraderInfoModel[]; error?: unknown }> {
        return this.#dataSource.getItems();
    }

    /**
     * Requests a test grader by ID (includes config schema).
     */
    async requestById(id: string): Promise<{ data?: TestGraderResponseModel; error?: unknown }> {
        return this.#dataSource.getById(id);
    }
}

export { UaiTestGraderItemRepository as api };
