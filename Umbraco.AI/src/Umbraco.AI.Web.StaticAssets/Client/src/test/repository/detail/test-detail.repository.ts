import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { UaiTestDetailServerDataSource } from "./test-detail.server.data-source.js";
import { UAI_TEST_DETAIL_STORE_CONTEXT } from "./test-detail.store.js";
import type { UaiTestDetailModel } from "../../types.js";
import { UAI_TEST_ENTITY_TYPE, UAI_TEST_ROOT_ENTITY_TYPE } from "../../constants.js";
import { UaiEntityActionEvent, dispatchActionEvent } from "../../../core/index.js";

/**
 * Repository for Test detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 * Dispatches entity action events after successful CRUD operations.
 */
export class UaiTestDetailRepository extends UmbDetailRepositoryBase<UaiTestDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiTestDetailServerDataSource, UAI_TEST_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiTestDetailModel) {
        const result = await super.create(model, null);
        if (!result.error && result.data) {
            dispatchActionEvent(this, UaiEntityActionEvent.created(result.data.unique, UAI_TEST_ENTITY_TYPE));
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: UAI_TEST_ROOT_ENTITY_TYPE,
                    unique: null,
                }),
            );
        }
        return result;
    }

    override async save(model: UaiTestDetailModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.updated(model.unique, UAI_TEST_ENTITY_TYPE));
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(unique, UAI_TEST_ENTITY_TYPE));
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: UAI_TEST_ROOT_ENTITY_TYPE,
                    unique: null,
                }),
            );
        }
        return result;
    }
}

export { UaiTestDetailRepository as api };
