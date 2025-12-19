import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
    UAiAgentExecutionServerDataSource,
    type UAiAgentExecutionRequest,
    type UAiAgentExecutionResponse
} from "./prompt-execution.server.data-source.js";

/**
 * Repository for prompt execution operations.
 * Provides a high-level API for executing Agents via the server-side endpoint.
 */
export class UAiAgentExecutionRepository extends UmbControllerBase {
    #dataSource: UAiAgentExecutionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UAiAgentExecutionServerDataSource(host);
    }

    /**
     * Executes a prompt by ID or alias and returns the AI response.
     * @param promptIdOrAlias - The prompt ID (GUID) or alias to execute.
     * @param request - The execution request containing context.
     * @param signal - Optional abort signal for cancellation.
     */
    async execute(
        promptIdOrAlias: string,
        request: UAiAgentExecutionRequest,
        signal?: AbortSignal
    ): Promise<{ data?: UAiAgentExecutionResponse; error?: unknown }> {
        return this.#dataSource.execute(promptIdOrAlias, request, signal);
    }
}
