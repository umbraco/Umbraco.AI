import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TestsService } from "../../../api/sdk.gen.js";
import type {
    RunTestRequestModel,
    RunTestBatchRequestModel,
    TestBatchResultsResponseModel,
} from "../../../api/types.gen.js";
import type { UaiTestExecutionResult } from "./types.js";

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
     * Returns execution result with per-variation metrics.
     */
    async runTest(
        idOrAlias: string,
        request?: RunTestRequestModel,
    ): Promise<{ data?: UaiTestExecutionResult; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.runTest({ path: { idOrAlias }, body: request }),
        );

        if (error || !data) {
            return { error };
        }

        // Cast until API types are regenerated
        return { data: data as unknown as UaiTestExecutionResult };
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

    /**
     * Gets the execution result (metrics breakdown) for a given execution ID.
     */
    async getExecutionResult(
        executionId: string,
    ): Promise<{ data?: UaiTestExecutionResult; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getExecutionResult({ path: { executionId } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data: data as unknown as UaiTestExecutionResult };
    }
}
