import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ChatService } from "../../api/sdk.gen.js";
import type { UaiChatRequest, UaiChatResult, UaiChatStreamChunk, UaiChatRole } from "../types.js";

/**
 * Server data source for chat operations.
 */
export class UaiChatServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Performs a chat completion.
     */
    async complete(request: UaiChatRequest): Promise<{ data?: UaiChatResult; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ChatService.completeChat({
                headers: { 
                    profileIdOrAlias: request.profileIdOrAlias ?? undefined
                },
                body: {
                    messages: request.messages.map(m => ({ role: m.role, content: m.content }))
                },
                signal: request.signal
            })
        );

        if (error || !data) {
            return { error };
        }

        return {
            data: {
                message: {
                    role: data.message.role as UaiChatRole,
                    content: data.message.content
                },
                finishReason: data.finishReason,
                usage: data.usage
            }
        };
    }

    /**
     * Performs a streaming chat completion.
     */
    async *stream(_request: UaiChatRequest): AsyncGenerator<UaiChatStreamChunk> {
        // TODO: Implement SSE streaming using createSseClient
        // Will use ChatService.streamChat endpoint
        throw new Error("Streaming not yet implemented");
    }
}
