import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UaiTestRunDetailServerDataSource } from "./test-run-detail.server.data-source.js";
import type {
    TestRunResponseModel,
    TestTranscriptResponseModel,
    TestRunComparisonResponseModel,
} from "../../../api/types.gen.js";

/**
 * Repository for test run detail operations.
 */
export class UaiTestRunDetailRepository extends UmbControllerBase {
    #dataSource: UaiTestRunDetailServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiTestRunDetailServerDataSource(host);
    }

    /**
     * Requests a test run by ID.
     */
    async requestById(id: string): Promise<{ data?: TestRunResponseModel; error?: unknown }> {
        return this.#dataSource.getById(id);
    }

    /**
     * Requests the transcript for a test run.
     */
    async requestTranscript(id: string): Promise<{ data?: TestTranscriptResponseModel; error?: unknown }> {
        return this.#dataSource.getTranscript(id);
    }

    /**
     * Requests the latest run for a test.
     */
    async requestLatest(testId: string): Promise<{ data?: TestRunResponseModel; error?: unknown }> {
        return this.#dataSource.getLatest(testId);
    }

    /**
     * Requests a comparison between two test runs.
     */
    async requestComparison(
        baselineRunId: string,
        comparisonRunId: string,
    ): Promise<{ data?: TestRunComparisonResponseModel; error?: unknown }> {
        return this.#dataSource.compare(baselineRunId, comparisonRunId);
    }

    /**
     * Requests setting a run as the baseline for a test.
     */
    async requestSetBaseline(testId: string, runId: string): Promise<{ error?: unknown }> {
        return this.#dataSource.setBaseline(testId, runId);
    }

    /**
     * Requests deletion of a test run.
     */
    async requestDelete(id: string): Promise<{ error?: unknown }> {
        return this.#dataSource.delete(id);
    }
}

export { UaiTestRunDetailRepository as api };
