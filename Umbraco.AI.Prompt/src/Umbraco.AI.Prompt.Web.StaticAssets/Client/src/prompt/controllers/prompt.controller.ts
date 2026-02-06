import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiPromptExecutionRepository } from "../repository/execution/prompt-execution.repository.js";
import type {
    UaiPromptContextItem,
    UaiPromptPropertyChange,
} from "../repository/execution/prompt-execution.server.data-source.js";

/**
 * Options for prompt execution.
 */
export interface UaiPromptExecuteOptions {
    /** Optional abort signal for cancellation. */
    signal?: AbortSignal;
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
 * Result of prompt execution.
 */
export interface UaiPromptExecuteResult {
    /** The generated response content. */
    content: string;
    /** Property changes to apply to the entity. */
    propertyChanges?: UaiPromptPropertyChange[];
}

/**
 * Controller for executing AI prompts.
 * This is the central API for prompt execution - all consumers should use this controller.
 * Calls the server-side execute endpoint which handles:
 * - Prompt resolution
 * - Template variable replacement
 * - (Future) Entity snapshot context
 * - AI chat completion
 *
 * @example
 * ```typescript
 * const controller = new UaiPromptController(this);
 *
 * const { data, error } = await controller.execute(
 *     "my-prompt-alias", // or prompt ID
 *     {
 *         entityId: "12345",
 *         entityType: "document",
 *         propertyAlias: "bodyText",
 *         context: {
 *             customValue: "some additional context"
 *         }
 *     }
 * );
 * ```
 *
 * @public
 */
export class UaiPromptController extends UmbControllerBase {
    #repository: UaiPromptExecutionRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiPromptExecutionRepository(this);
    }

    /**
     * Executes a prompt by ID or alias and returns the AI response.
     * Calls the server-side execute endpoint which handles:
     * - Prompt resolution
     * - Scope validation
     * - Template variable replacement
     * - (Future) Entity snapshot context
     * - AI chat completion
     *
     * @param promptIdOrAlias - The prompt ID (GUID) or alias to execute.
     * @param options - Configuration including entity context (required for scope validation).
     * @returns The AI response content or error.
     */
    async execute(
        promptIdOrAlias: string,
        options: UaiPromptExecuteOptions,
    ): Promise<{ data?: UaiPromptExecuteResult; error?: Error }> {
        try {
            const { data, error } = await this.#repository.execute(
                promptIdOrAlias,
                {
                    entityId: options.entityId,
                    entityType: options.entityType,
                    propertyAlias: options.propertyAlias,
                    culture: options.culture,
                    segment: options.segment,
                    context: options.context,
                },
                options.signal,
            );

            if (error) {
                return {
                    error: error instanceof Error ? error : new Error("Failed to execute prompt"),
                };
            }

            if (data) {
                return {
                    data: {
                        content: data.content,
                        propertyChanges: data.propertyChanges,
                    },
                };
            }

            return { error: new Error("No response received") };
        } catch (err) {
            if ((err as Error)?.name === "AbortError") {
                return { error: err as Error };
            }
            return {
                error: err instanceof Error ? err : new Error("Failed to execute prompt"),
            };
        }
    }
}
