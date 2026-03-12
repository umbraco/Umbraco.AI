import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TestsService } from "../../../api/sdk.gen.js";
import type { TestGraderInfoModel, TestGraderResponseModel } from "../../../api/types.gen.js";

/**
 * Server data source for fetching test grader items.
 */
export class UaiTestGraderItemServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches all available test graders.
     */
    async getItems(): Promise<{ data?: TestGraderInfoModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(this.#host, TestsService.getAllTestGraders());

        if (error || !data) {
            return { error };
        }

        return { data };
    }

    /**
     * Fetches a test grader by ID (includes config schema).
     */
    async getById(id: string): Promise<{ data?: TestGraderResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getTestGraderById({ path: { id } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }
}
