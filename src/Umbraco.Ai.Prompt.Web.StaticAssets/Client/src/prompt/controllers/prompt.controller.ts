import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiChatController, type UaiChatMessage } from "@umbraco-ai/core";

/**
 * Options for prompt execution.
 */
export interface UaiPromptExecuteOptions {
    /** Optional profile ID or alias to use for the AI completion. */
    profileIdOrAlias?: string;
    /** Optional abort signal for cancellation. */
    signal?: AbortSignal;
    /** Context object for template variable replacement. Supports dot notation for nested properties. */
    context?: Record<string, unknown>;
}

/**
 * Result of prompt execution.
 */
export interface UaiPromptExecuteResult {
    /** The generated response content. */
    content: string;
}

/**
 * Controller for executing AI prompts with template variable support.
 * Wraps the UaiChatController to provide a simpler API for prompt execution.
 *
 * @example
 * ```typescript
 * const controller = new UaiPromptController(this);
 *
 * const { data, error } = await controller.execute(
 *     "Summarize {{document.name}}: {{document.content}}",
 *     {
 *         context: {
 *             document: { name: "My Article", content: "..." }
 *         }
 *     }
 * );
 * ```
 *
 * @public
 */
export class UaiPromptController extends UmbControllerBase {
    #chatController: UaiChatController;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#chatController = new UaiChatController(this);
    }

    /**
     * Gets a nested value from an object using dot notation.
     * @param obj - The object to traverse.
     * @param path - The dot-notation path (e.g., "user.name" or "document.properties.title").
     * @returns The string value or undefined if not found.
     */
    #getNestedValue(obj: Record<string, unknown>, path: string): string | undefined {
        const value = path.split('.').reduce<unknown>((current, key) => {
            if (current && typeof current === 'object' && key in current) {
                return (current as Record<string, unknown>)[key];
            }
            return undefined;
        }, obj);

        if (value === undefined || value === null) {
            return undefined;
        }
        return String(value);
    }

    /**
     * Replaces {{variable}} placeholders in a template with values from context.
     * Supports dot notation for nested properties (e.g., {{user.name}}).
     * Unmatched placeholders are kept as-is.
     * @param template - The template string with {{variable}} placeholders.
     * @param context - The context object containing replacement values.
     * @returns The template with placeholders replaced.
     */
    #replaceVariables(template: string, context: Record<string, unknown>): string {
        return template.replace(/\{\{([\w.]+)\}\}/g, (match, path) => {
            return this.#getNestedValue(context, path) ?? match;
        });
    }

    /**
     * Executes a prompt and returns the AI response.
     * @param promptContent - The prompt content to execute. May contain {{variable}} placeholders.
     * @param options - Optional configuration (profile ID/alias, abort signal, context for variable replacement).
     * @returns The AI response content or error.
     */
    async execute(
        promptContent: string,
        options?: UaiPromptExecuteOptions
    ): Promise<{ data?: UaiPromptExecuteResult; error?: Error }> {
        const resolvedContent = options?.context
            ? this.#replaceVariables(promptContent, options.context)
            : promptContent;

        const messages: UaiChatMessage[] = [
            { role: 'user', content: resolvedContent }
        ];

        try {
            const { data, error } = await this.#chatController.complete(messages, {
                profileIdOrAlias: options?.profileIdOrAlias,
                signal: options?.signal,
            });

            if (error) {
                return {
                    error: error instanceof Error
                        ? error
                        : new Error('Failed to execute prompt')
                };
            }

            if (data) {
                return {
                    data: { content: data.message.content }
                };
            }

            return { error: new Error('No response received') };
        } catch (err) {
            if ((err as Error)?.name === 'AbortError') {
                return { error: err as Error };
            }
            return {
                error: err instanceof Error
                    ? err
                    : new Error('Failed to execute prompt')
            };
        }
    }
}
