import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
    UaiPromptExecutionServerDataSource,
    type UaiPromptExecutionRequest,
    type UaiPromptExecutionResponse
} from "./prompt-execution.server.data-source.js";

/**
 * Repository for prompt execution operations.
 * Provides a high-level API for executing prompts via the server-side endpoint.
 */
export class UaiPromptExecutionRepository extends UmbControllerBase {
    #dataSource: UaiPromptExecutionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiPromptExecutionServerDataSource(host);
    }

    /**
     * Executes a prompt by ID or alias and returns the AI response.
     * @param promptIdOrAlias - The prompt ID (GUID) or alias to execute.
     * @param request - The execution request containing context.
     * @param signal - Optional abort signal for cancellation.
     */
    async execute(
        promptIdOrAlias: string,
        request: UaiPromptExecutionRequest,
        signal?: AbortSignal
    ): Promise<{ data?: UaiPromptExecutionResponse; error?: unknown }> {
        return this.#dataSource.execute(promptIdOrAlias, request, signal);
    }
}
