import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { UaiEntityActionEvent, dispatchActionEvent } from "@umbraco-ai/core";
import { UaiOrchestrationDetailServerDataSource } from "./orchestration-detail.server.data-source.js";
import { UAI_ORCHESTRATION_DETAIL_STORE_CONTEXT } from "./orchestration-detail.store.js";
import type { UaiOrchestrationDetailModel } from "../../types.js";
import { UAI_ORCHESTRATION_ENTITY_TYPE, UAI_ORCHESTRATION_ROOT_ENTITY_TYPE } from "../../constants.js";

/**
 * Repository for Orchestration detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 * Dispatches entity action events after successful CRUD operations.
 */
export class UaiOrchestrationDetailRepository extends UmbDetailRepositoryBase<UaiOrchestrationDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiOrchestrationDetailServerDataSource, UAI_ORCHESTRATION_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiOrchestrationDetailModel) {
        const result = await super.create(model, null);
        if (!result.error && result.data) {
            dispatchActionEvent(
                this,
                UaiEntityActionEvent.created(result.data.unique, UAI_ORCHESTRATION_ENTITY_TYPE),
            );
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: UAI_ORCHESTRATION_ROOT_ENTITY_TYPE,
                    unique: null,
                }),
            );
        }
        return result;
    }

    override async save(model: UaiOrchestrationDetailModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(
                this,
                UaiEntityActionEvent.updated(model.unique, UAI_ORCHESTRATION_ENTITY_TYPE),
            );
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(unique, UAI_ORCHESTRATION_ENTITY_TYPE));
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: UAI_ORCHESTRATION_ROOT_ENTITY_TYPE,
                    unique: null,
                }),
            );
        }
        return result;
    }
}

export { UaiOrchestrationDetailRepository as api };
