import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiChatServerDataSource } from "./chat.server.data-source.js";
import type { UaiChatRequest, UaiChatResult } from "../types.js";

/**
 * Repository for chat operations.
 */
export class UaiChatRepository extends UmbControllerBase {
    #dataSource: UaiChatServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiChatServerDataSource(host);
    }

    /**
     * Performs a chat completion.
     */
    async complete(request: UaiChatRequest): Promise<{ data?: UaiChatResult; error?: unknown }> {
        return this.#dataSource.complete(request);
    }
}
