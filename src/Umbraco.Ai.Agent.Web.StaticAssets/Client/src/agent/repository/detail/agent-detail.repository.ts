import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { UaiEntityActionEvent, dispatchActionEvent } from "@umbraco-ai/core";
import { UAiAgentDetailServerDataSource } from "./agent-detail.server.data-source.js";
import { UAI_AGENT_DETAIL_STORE_CONTEXT } from "./agent-detail.store.js";
import type { UAiAgentDetailModel } from "../../types.js";
import { UAI_AGENT_ENTITY_TYPE, UAI_AGENT_ROOT_ENTITY_TYPE } from "../../constants.js";

/**
 * Repository for Agent detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 * Dispatches entity action events after successful CRUD operations.
 */
export class UAiAgentDetailRepository extends UmbDetailRepositoryBase<UAiAgentDetailModel> {
    
    constructor(host: UmbControllerHost) {
        super(host, UAiAgentDetailServerDataSource, UAI_AGENT_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UAiAgentDetailModel) {
        const result = await super.create(model, null);
        if (!result.error && result.data) {
            dispatchActionEvent(this, UaiEntityActionEvent.created(result.data.unique, UAI_AGENT_ENTITY_TYPE));
            dispatchActionEvent(this, new UmbRequestReloadChildrenOfEntityEvent({
                entityType: UAI_AGENT_ROOT_ENTITY_TYPE,
                unique: null,
            }));
        }
        return result;
    }

    override async save(model: UAiAgentDetailModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.updated(model.unique, UAI_AGENT_ENTITY_TYPE));
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(unique, UAI_AGENT_ENTITY_TYPE));
            dispatchActionEvent(this, new UmbRequestReloadChildrenOfEntityEvent({
                entityType: UAI_AGENT_ROOT_ENTITY_TYPE,
                unique: null,
            }));
        }
        return result;
    }
}

export { UAiAgentDetailRepository as api };
