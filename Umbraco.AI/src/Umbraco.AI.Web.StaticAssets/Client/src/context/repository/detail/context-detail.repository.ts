import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { UaiContextDetailServerDataSource } from "./context-detail.server.data-source.js";
import { UAI_CONTEXT_DETAIL_STORE_CONTEXT } from "./context-detail.store.js";
import type { UaiContextDetailModel } from "../../types.js";
import { UAI_CONTEXT_ENTITY_TYPE, UAI_CONTEXT_ROOT_ENTITY_TYPE } from "../../constants.js";
import { UaiEntityActionEvent, dispatchActionEvent } from "../../../core/index.js";

/**
 * Repository for Context detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 * Dispatches entity action events after successful CRUD operations.
 */
export class UaiContextDetailRepository extends UmbDetailRepositoryBase<UaiContextDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiContextDetailServerDataSource, UAI_CONTEXT_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiContextDetailModel) {
        const result = await super.create(model, null);
        if (!result.error && result.data) {
            dispatchActionEvent(this, UaiEntityActionEvent.created(result.data.unique, UAI_CONTEXT_ENTITY_TYPE));
            dispatchActionEvent(this, new UmbRequestReloadChildrenOfEntityEvent({
                entityType: UAI_CONTEXT_ROOT_ENTITY_TYPE,
                unique: null,
            }));
        }
        return result;
    }

    override async save(model: UaiContextDetailModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.updated(model.unique, UAI_CONTEXT_ENTITY_TYPE));
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(unique, UAI_CONTEXT_ENTITY_TYPE));
            dispatchActionEvent(this, new UmbRequestReloadChildrenOfEntityEvent({
                entityType: UAI_CONTEXT_ROOT_ENTITY_TYPE,
                unique: null,
            }));
        }
        return result;
    }
}

export { UaiContextDetailRepository as api };
