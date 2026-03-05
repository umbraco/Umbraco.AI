import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiTestExecutionServerDataSource } from "./test-execution.server.data-source.js";
import type {
    RunTestRequestModel,
    TestBatchResultsResponseModel,
} from "../../../api/types.gen.js";
import type { UaiTestExecutionResult } from "./types.js";

/**
 * Repository for test execution operations.
 */
export class UaiTestExecutionRepository extends UmbControllerBase {
    #dataSource: UaiTestExecutionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiTestExecutionServerDataSource(host);
    }

    /**
     * Requests execution of a single test.
     * Returns execution result with per-variation metrics.
     */
    async requestRunTest(
        idOrAlias: string,
        request?: RunTestRequestModel,
    ): Promise<{ data?: UaiTestExecutionResult; error?: unknown }> {
        return this.#dataSource.runTest(idOrAlias, request);
    }

    /**
     * Requests execution of tests by IDs.
     */
    async requestRunByIds(
        testIds: string[],
        profileIdOverride?: string,
        contextIdsOverride?: string[],
    ): Promise<{ data?: TestBatchResultsResponseModel; error?: unknown }> {
        return this.#dataSource.runBatch({
            testIds,
            profileIdOverride,
            contextIdsOverride,
        });
    }

    /**
     * Requests the execution result (metrics breakdown) for a given execution ID.
     */
    async requestExecutionResult(
        executionId: string,
    ): Promise<{ data?: UaiTestExecutionResult; error?: unknown }> {
        return this.#dataSource.getExecutionResult(executionId);
    }
}

export { UaiTestExecutionRepository as api };
