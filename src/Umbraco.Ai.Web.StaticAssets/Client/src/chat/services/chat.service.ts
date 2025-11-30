import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiChatRepository } from "../repository/chat.repository.js";
import type { UaiChatMessage, UaiChatOptions, UaiChatResult, UaiChatStreamChunk } from "../types.js";

/**
 * Public API for performing chat completions.
 * @public
 */
export class UaiChatService extends UmbControllerBase {
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
            profileId: options?.profile,
            messages,
            signal: options?.signal
        });
    }

    /**
     * Performs a streaming chat completion.
     * @param messages - The conversation messages.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns AsyncGenerator yielding content chunks.
     */
    stream(
        messages: UaiChatMessage[],
        options?: UaiChatOptions
    ): AsyncGenerator<UaiChatStreamChunk> {
        return this.#repository.stream({
            profileId: options?.profile,
            messages,
            signal: options?.signal
        });
    }
}