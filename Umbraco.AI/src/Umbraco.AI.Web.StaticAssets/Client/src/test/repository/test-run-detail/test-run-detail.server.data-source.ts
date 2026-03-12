import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { TestsService } from "../../../api/sdk.gen.js";
import type {
    TestRunResponseModel,
    TestTranscriptResponseModel,
    TestRunComparisonResponseModel,
} from "../../../api/types.gen.js";

/**
 * Server data source for test run detail operations.
 */
export class UaiTestRunDetailServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Fetches a test run by ID.
     */
    async getById(id: string): Promise<{ data?: TestRunResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getTestRunById({ path: { id } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }

    /**
     * Fetches the transcript for a test run.
     */
    async getTranscript(id: string): Promise<{ data?: TestTranscriptResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getTestRunTranscript({ path: { id } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }

    /**
     * Fetches the latest run for a test.
     */
    async getLatest(testId: string): Promise<{ data?: TestRunResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.getLatestTestRun({ path: { testId } }),
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }

    /**
     * Compares two test runs.
     */
    async compare(
        baselineRunId: string,
        comparisonRunId: string,
    ): Promise<{ data?: TestRunComparisonResponseModel; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            TestsService.compareTestRuns({
                body: {
                    baselineTestRunId: baselineRunId,
                    comparisonTestRunId: comparisonRunId,
                },
            }),
        );

        if (error || !data) {
            return { error };
        }

        return { data };
    }

    /**
     * Sets a run as the baseline for a test.
     */
    async setBaseline(testId: string, runId: string): Promise<{ error?: unknown }> {
        const { error } = await tryExecute(
            this.#host,
            TestsService.setBaselineTestRun({ path: { testId, testRunId: runId } }),
        );

        if (error) {
            return { error };
        }

        return {};
    }

    /**
     * Deletes a test run.
     */
    async delete(id: string): Promise<{ error?: unknown }> {
        const { error } = await tryExecute(
            this.#host,
            TestsService.deleteTestRun({ path: { id } }),
        );

        if (error) {
            return { error };
        }

        return {};
    }
}
