import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiChatRepository } from "../repository/chat.repository.js";
import type { UaiChatMessage, UaiChatOptions, UaiChatResult } from "../types.js";

/**
 * Public API for performing chat completions.
 * @public
 */
export class UaiChatController extends UmbControllerBase {
    #repository: UaiChatRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiChatRepository(host);
    }

    /**
     * Performs a chat completion.
     * @param messages - The conversation messages.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns The AI response or error.
     */
    async complete(
        messages: UaiChatMessage[],
        options?: UaiChatOptions
    ): Promise<{ data?: UaiChatResult; error?: unknown }> {
        return this.#repository.complete({
            profileIdOrAlias: options?.profileIdOrAlias,
            messages,
            signal: options?.signal
        });
    }
}