import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ConversationsService } from "../../api/sdk.gen.js";
import { copilotWorkspaceClientReady } from "../../app.js";
import type {
    CreateConversationRequestModel,
    UaiConversationCollectionFilter,
    UpdateConversationRequestModel,
} from "../types.js";

/**
 * Server data source for conversation CRUD, wrapping the generated
 * `ConversationsService`. Each call awaits {@link copilotWorkspaceClientReady}
 * so the client already carries auth, and routes through `tryExecute` for
 * consistent error normalization + server-notification surfacing.
 */
export class UaiConversationServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getCollection(filter: UaiConversationCollectionFilter) {
        await copilotWorkspaceClientReady;
        return tryExecute(
            this.#host,
            ConversationsService.getAll({
                query: {
                    projectId: filter.projectId,
                    search: filter.search,
                    includeArchived: filter.includeArchived,
                    skip: filter.skip ?? 0,
                    take: filter.take ?? 200,
                },
            }),
        );
    }

    async create(request: CreateConversationRequestModel) {
        await copilotWorkspaceClientReady;
        return tryExecute(this.#host, ConversationsService.create({ body: request }));
    }

    async update(id: string, request: UpdateConversationRequestModel) {
        await copilotWorkspaceClientReady;
        return tryExecute(this.#host, ConversationsService.update({ path: { id }, body: request }));
    }

    async delete(id: string) {
        await copilotWorkspaceClientReady;
        return tryExecute(this.#host, ConversationsService.delete({ path: { id } }));
    }
}
