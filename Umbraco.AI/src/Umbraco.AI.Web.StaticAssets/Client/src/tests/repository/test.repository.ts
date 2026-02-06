import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type {
    TestItemResponseModel,
    TestResponseModel,
    CreateTestRequestModel,
    UpdateTestRequestModel,
    RunTestRequestModel,
    TestRunResponseModel,
} from "../../api/client/index.js";
import { TestService } from "../../api/client/index.js";

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
    async getAll(
        filter?: string,
        tags?: string,
        skip: number = 0,
        take: number = 100,
    ): Promise<{ items: TestItemResponseModel[]; total: number }> {
        const response = await TestService.getAllTests(filter, tags, skip, take);
        return {
            items: response.items ?? [],
            total: response.total ?? 0,
        };
    }

    /**
     * Get a test by ID or alias.
     */
    async getById(idOrAlias: string): Promise<TestResponseModel | null> {
        try {
            return await TestService.getTestByIdOrAlias(idOrAlias);
        } catch {
            return null;
        }
    }

    /**
     * Create a new test.
     */
    async create(model: CreateTestRequestModel): Promise<string> {
        return await TestService.createTest(model);
    }

    /**
     * Update a test.
     */
    async update(idOrAlias: string, model: UpdateTestRequestModel): Promise<void> {
        await TestService.updateTest(idOrAlias, model);
    }

    /**
     * Delete a test.
     */
    async delete(idOrAlias: string): Promise<void> {
        await TestService.deleteTest(idOrAlias);
    }

    /**
     * Run a test.
     */
    async run(idOrAlias: string, model?: RunTestRequestModel): Promise<TestRunResponseModel> {
        return await TestService.runTest(idOrAlias, model);
    }
}
