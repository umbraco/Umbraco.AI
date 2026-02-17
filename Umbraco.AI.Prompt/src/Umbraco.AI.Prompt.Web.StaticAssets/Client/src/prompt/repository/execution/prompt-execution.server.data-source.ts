import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { PromptsService } from "../../../api/index.js";

/**
 * Context item for passing data to AI operations.
 * Matches backend AIRequestContextItem.
 */
export interface UaiPromptContextItem {
    /** Human-readable description */
    description: string;
    /** The context data (any JSON-serializable value) */
    value?: string;
}

/**
 * Represents a value change to be applied to the entity.
 * Matches backend ValueChangeModel and Core's UaiValueChange.
 */
export interface UaiPromptValueChange {
    /** JSON path to the value (e.g., "title", "price.amount"). */
    path: string;
    /** The new value to set. */
    value: unknown;
    /** The culture for variant content. */
    culture?: string;
    /** The segment for segmented content. */
    segment?: string;
}

/**
 * Request model for prompt execution.
 */
export interface UaiPromptExecutionRequest {
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
    /** Flexible context items array for passing frontend context to processors. */
    context?: UaiPromptContextItem[];
}

/**
 * A single result option that can be displayed and optionally applied.
 */
export interface UaiPromptResultOption {
    label: string;
    displayValue: string;
    description?: string | null;
    valueChange?: UaiPromptValueChange | null;
}

/**
 * Response model for prompt execution.
 */
export interface UaiPromptExecutionResponse {
    /** The generated response content. */
    content: string;
    /** Token usage information. */
    usage?: {
        inputTokens?: number;
        outputTokens?: number;
        totalTokens?: number;
    };
    /**
     * Available result options. Always present, may be empty.
     * - Empty array: Informational only
     * - Single item: One value to insert
     * - Multiple items: User selects one
     */
    resultOptions: UaiPromptResultOption[];
}

/**
 * Server data source for prompt execution operations.
 */
export class UaiPromptExecutionServerDataSource {
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
        request: UaiPromptExecutionRequest,
        _signal?: AbortSignal,
    ): Promise<{ data?: UaiPromptExecutionResponse; error?: unknown }> {
        // Build the body with context items array (typed as any to bypass generated type mismatch)
        const body = {
            entityId: request.entityId,
            entityType: request.entityType,
            propertyAlias: request.propertyAlias,
            culture: request.culture,
            segment: request.segment,
            context: request.context,
        } as any;

        const { data, error } = await tryExecute(
            this.#host,
            PromptsService.executePrompt({
                path: { promptIdOrAlias },
                body,
            }),
        );

        if (error || !data) {
            return { error };
        }

        return {
            data: {
                content: data.content,
                usage: data.usage
                    ? {
                          inputTokens: data.usage.inputTokens ?? undefined,
                          outputTokens: data.usage.outputTokens ?? undefined,
                          totalTokens: data.usage.totalTokens ?? undefined,
                      }
                    : undefined,
                resultOptions:
                    data.resultOptions?.map((option) => ({
                        label: option.label,
                        displayValue: option.displayValue,
                        description: option.description ?? undefined,
                        valueChange: option.valueChange
                            ? {
                                  path: option.valueChange.path,
                                  value: option.valueChange.value,
                                  culture: option.valueChange.culture ?? undefined,
                                  segment: option.valueChange.segment ?? undefined,
                              }
                            : undefined,
                    })) ?? [],
            },
        };
    }
}
