import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRequestReloadChildrenOfEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { UaiGuardrailDetailServerDataSource } from "./guardrail-detail.server.data-source.js";
import { UAI_GUARDRAIL_DETAIL_STORE_CONTEXT } from "./guardrail-detail.store.js";
import type { UaiGuardrailDetailModel } from "../../types.js";
import { UAI_GUARDRAIL_ENTITY_TYPE, UAI_GUARDRAIL_ROOT_ENTITY_TYPE } from "../../constants.js";
import { UaiEntityActionEvent, dispatchActionEvent } from "../../../core/index.js";

/**
 * Repository for Guardrail detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 * Dispatches entity action events after successful CRUD operations.
 */
export class UaiGuardrailDetailRepository extends UmbDetailRepositoryBase<UaiGuardrailDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiGuardrailDetailServerDataSource, UAI_GUARDRAIL_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiGuardrailDetailModel) {
        const result = await super.create(model, null);
        if (!result.error && result.data) {
            dispatchActionEvent(this, UaiEntityActionEvent.created(result.data.unique, UAI_GUARDRAIL_ENTITY_TYPE));
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: UAI_GUARDRAIL_ROOT_ENTITY_TYPE,
                    unique: null,
                }),
            );
        }
        return result;
    }

    override async save(model: UaiGuardrailDetailModel) {
        const result = await super.save(model);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.updated(model.unique, UAI_GUARDRAIL_ENTITY_TYPE));
        }
        return result;
    }

    override async delete(unique: string) {
        const result = await super.delete(unique);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(unique, UAI_GUARDRAIL_ENTITY_TYPE));
            dispatchActionEvent(
                this,
                new UmbRequestReloadChildrenOfEntityEvent({
                    entityType: UAI_GUARDRAIL_ROOT_ENTITY_TYPE,
                    unique: null,
                }),
            );
        }
        return result;
    }
}

export { UaiGuardrailDetailRepository as api };
