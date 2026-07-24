import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiEntityActionEvent, dispatchActionEvent } from "@umbraco-ai/core";
import { UaiProjectServerDataSource } from "./project.server.data-source.js";
import { UAI_PROJECT_ENTITY_TYPE } from "../../constants.js";
import type { ProjectRequestModel } from "../../api/types.gen.js";

/**
 * Stateless repository for project CRUD (mirrors {@link UaiConversationRepository}). Mutations dispatch
 * a `UaiEntityActionEvent` on the shared action-event bus so reactive observers — notably the sidebar
 * context — refresh without a manual reload. The reactive project list itself is owned by the sidebar
 * context (the only consumer that needs it), not by this repository, so consumers can freely `new` it.
 */
export class UaiProjectRepository extends UmbRepositoryBase {
    #source: UaiProjectServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#source = new UaiProjectServerDataSource(host);
    }

    async requestCollection() {
        return this.#source.getCollection();
    }

    async requestById(id: string) {
        return this.#source.getById(id);
    }

    async create(request: ProjectRequestModel) {
        const result = await this.#source.create(request);
        if (!result.error && result.data?.id) {
            dispatchActionEvent(this, UaiEntityActionEvent.created(result.data.id, UAI_PROJECT_ENTITY_TYPE));
        }
        return result;
    }

    async update(id: string, request: ProjectRequestModel) {
        const result = await this.#source.update(id, request);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.updated(id, UAI_PROJECT_ENTITY_TYPE));
        }
        return result;
    }

    async delete(id: string) {
        const result = await this.#source.delete(id);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(id, UAI_PROJECT_ENTITY_TYPE));
        }
        return result;
    }
}
