import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AgentsService } from "../../../api/index.js";
import type { PromptExecutionRequestModel } from "../../../api/types.gen.js";

/**
 * Request model for prompt execution.
 */
export interface UAiAgentExecutionRequest {
    /** The entity ID for context. Required for scope validation. */
    entityId: string;
    /** The entity type (e.g., "document", "media"). Required for scope validation. */
    entityType: string;
    /** The property alias being edited. Required for scope validation. */
    propertyAlias: string;
    /** The culture variant. */
    culture?: string;
    /** The segment variant. */
    segment?: string;
    /** Local content model for snapshot (future use). */
    localContent?: Record<string, unknown>;
    /** Additional context variables. */
    context?: Record<string, unknown>;
}

/**
 * Response model for prompt execution.
 */
export interface UAiAgentExecutionResponse {
    /** The generated response content. */
    content: string;
    /** Token usage information. */
    usage?: {
        inputTokens?: number;
        outputTokens?: number;
        totalTokens?: number;
    };
}

/**
 * Server data source for prompt execution operations.
 */
export class UAiAgentExecutionServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Executes a prompt by ID or alias and returns the AI response.
     * @param promptIdOrAlias - The prompt ID (GUID) or alias to execute.
     * @param request - The execution request containing context.
     * @param _signal - Optional abort signal for cancellation (reserved for future use).
     */
    async execute(
        promptIdOrAlias: string,
        request: UAiAgentExecutionRequest,
        _signal?: AbortSignal
    ): Promise<{ data?: UAiAgentExecutionResponse; error?: unknown }> {
        const body: PromptExecutionRequestModel = {
            entityId: request.entityId,
            entityType: request.entityType,
            propertyAlias: request.propertyAlias,
            culture: request.culture,
            segment: request.segment,
            localContent: request.localContent,
            context: request.context,
        };

        const { data, error } = await tryExecute(
            this.#host,
            AgentsService.executePrompt({
                path: { promptIdOrAlias },
                body,
            })
        );

        if (error || !data) {
            return { error };
        }

        return {
            data: {
                content: data.content,
                usage: data.usage ? {
                    inputTokens: data.usage.inputTokens ?? undefined,
                    outputTokens: data.usage.outputTokens ?? undefined,
                    totalTokens: data.usage.totalTokens ?? undefined,
                } : undefined,
            },
        };
    }
}
