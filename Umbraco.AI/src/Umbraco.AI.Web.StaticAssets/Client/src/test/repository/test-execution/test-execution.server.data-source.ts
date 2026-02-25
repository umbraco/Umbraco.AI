import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TestsService } from "../../../api/sdk.gen.js";
import type {
    RunTestRequestModel,
    RunTestBatchRequestModel,
    TestMetricsResponseModel,
    TestBatchResultsResponseModel,
} from "../../../api/types.gen.js";

/**
 * Server data source for test execution operations.
 */
export class UaiTestExecutionServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Runs a single test by ID or alias.
     */
    async runTest(
        idOrAlias: string,
        request?: RunTestRequestModel,
    ): Promise<{ data?: TestMetricsResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.runTest({ path: { idOrAlias }, body: request }),
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }

    /**
     * Runs multiple tests in batch.
     */
    async runBatch(
        request: RunTestBatchRequestModel,
    ): Promise<{ data?: TestBatchResultsResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.runTestBatch({ body: request }),
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }
}
