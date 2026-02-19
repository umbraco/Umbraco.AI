import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
    TestItemResponseModel,
    TestResponseModel,
    CreateTestRequestModel,
    UpdateTestRequestModel,
    RunTestRequestModel,
    TestMetricsResponseModel,
    TestRunResponseModel,
    TestBatchResultsResponseModel,
    RunTestBatchRequestModel,
    TestFeatureInfoModel,
    TestGraderInfoModel,
    TestRunComparisonResponseModel,
} from "../../api/types.gen.js";
import { TestsService } from "../../api/sdk.gen.js";

/**
 * Repository for managing AI tests.
 * Wraps the generated API client with Umbraco patterns.
 */
export class AITestRepository extends UmbControllerBase {
    constructor(host: UmbControllerHost) {
        super(host);
    }

    /**
     * Get all tests (paged).
     */
    async getAllTests(
        filter?: string,
        tags?: string,
        skip: number = 0,
        take: number = 100,
    ): Promise<{ items: TestItemResponseModel[]; total: number }> {
        const { data } = await TestsService.getAllTests({
            query: { filter, tags, skip, take },
        });
        return {
            items: data?.items ?? [],
            total: data?.total ?? 0,
        };
    }

    /**
     * Get a test by ID or alias.
     */
    async getTestByIdOrAlias(idOrAlias: string): Promise<TestResponseModel | null> {
        try {
            const { data } = await TestsService.getTestByIdOrAlias({
                path: { idOrAlias },
            });
            return data ?? null;
        } catch {
            return null;
        }
    }

    /**
     * Create a new test.
     */
    async createTest(model: CreateTestRequestModel): Promise<string> {
        const { data } = await TestsService.createTest({
            body: model,
        });
        return (data as string) ?? "";
    }

    /**
     * Update a test.
     */
    async updateTest(idOrAlias: string, model: UpdateTestRequestModel): Promise<void> {
        await TestsService.updateTest({
            path: { idOrAlias },
            body: model,
        });
    }

    /**
     * Delete a test.
     */
    async deleteTest(idOrAlias: string): Promise<void> {
        await TestsService.deleteTest({
            path: { idOrAlias },
        });
    }

    /**
     * Run a test and get metrics.
     * Executes the test N times and returns aggregate metrics with pass@k.
     */
    async runTest(
        idOrAlias: string,
        request?: RunTestRequestModel
    ): Promise<TestMetricsResponseModel> {
        const { data } = await TestsService.runTest({
            path: { idOrAlias },
            body: request,
        });
        return data!;
    }

    /**
     * Run multiple tests in batch.
     */
    async runBatch(request: RunTestBatchRequestModel): Promise<TestBatchResultsResponseModel> {
        const { data } = await TestsService.runBatch({
            body: request,
        });
        return data!;
    }

    /**
     * Run tests by tags.
     */
    async runByTags(
        tags: string[],
        profileIdOverride?: string,
        contextIdsOverride?: string[]
    ): Promise<TestBatchResultsResponseModel> {
        const { data } = await TestsService.runByTags({
            body: {
                tags,
                profileIdOverride,
                contextIdsOverride,
            },
        });
        return data!;
    }

    /**
     * Get all test runs (paged).
     */
    async getAllRuns(
        testId?: string,
        skip: number = 0,
        take: number = 100
    ): Promise<{ items: TestRunResponseModel[]; total: number }> {
        const { data } = await TestsService.getAll3({
            query: { testId, skip, take },
        });
        return {
            items: data?.items ?? [],
            total: data?.total ?? 0,
        };
    }

    /**
     * Get a test run by ID.
     */
    async getRunById(id: string): Promise<TestRunResponseModel | null> {
        try {
            const { data } = await TestsService.getById({
                path: { id },
            });
            return data ?? null;
        } catch {
            return null;
        }
    }

    /**
     * Get the latest run for a test.
     */
    async getLatestRun(testId: string): Promise<TestRunResponseModel | null> {
        try {
            const { data } = await TestsService.getLatest({
                path: { testId },
            });
            return data ?? null;
        } catch {
            return null;
        }
    }

    /**
     * Compare two test runs.
     */
    async compareRuns(
        baselineRunId: string,
        comparisonRunId: string
    ): Promise<TestRunComparisonResponseModel> {
        const { data } = await TestsService.compareRuns({
            body: {
                baselineRunId,
                comparisonRunId,
            },
        });
        return data!;
    }

    /**
     * Set a run as the baseline for comparison.
     */
    async setBaseline(testId: string, runId: string): Promise<void> {
        await TestsService.setBaseline({
            path: { testId, runId },
        });
    }

    /**
     * Delete a test run.
     */
    async deleteRun(id: string): Promise<void> {
        await TestsService.delete({
            path: { id },
        });
    }

    /**
     * Get all available test features.
     */
    async getAllTestFeatures(): Promise<TestFeatureInfoModel[]> {
        const { data } = await TestsService.getAll();
        return data ?? [];
    }

    /**
     * Get all available test graders.
     */
    async getAllTestGraders(): Promise<TestGraderInfoModel[]> {
        const { data } = await TestsService.getAll2();
        return data ?? [];
    }
}
