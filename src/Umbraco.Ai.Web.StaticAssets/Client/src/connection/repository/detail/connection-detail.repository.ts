import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiConnectionDetailServerDataSource } from "./connection-detail.server.data-source.js";
import { UAI_CONNECTION_DETAIL_STORE_CONTEXT } from "./connection-detail.store.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import { UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";
import { UaiEntityActionEvent, dispatchActionEvent } from "../../../core/index.js";

/**
 * Repository for Connection detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 * Dispatches entity action events after successful CRUD operations.
 */
export class UaiConnectionDetailRepository extends UmbDetailRepositoryBase<UaiConnectionDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiConnectionDetailServerDataSource, UAI_CONNECTION_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiConnectionDetailModel) {
        const result = await super.create(model, null);
        if (!result.error && result.data) {
            dispatchActionEvent(this, UaiEntityActionEvent.created(result.data.unique, UAI_CONNECTION_ENTITY_TYPE));
        }
        return result;
    }

    override async save(model: UaiConnectionDetailModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.updated(model.unique, UAI_CONNECTION_ENTITY_TYPE));
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(unique, UAI_CONNECTION_ENTITY_TYPE));
        }
        return result;
    }
}

export { UaiConnectionDetailRepository as api };
