import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiPromptDetailServerDataSource } from "./prompt-detail.server.data-source.js";
import { UAI_PROMPT_DETAIL_STORE_CONTEXT } from "./prompt-detail.store.js";
import type { UaiPromptDetailModel } from "../../types.js";

/**
 * Repository for Prompt detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 */
export class UaiPromptDetailRepository extends UmbDetailRepositoryBase<UaiPromptDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiPromptDetailServerDataSource, UAI_PROMPT_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiPromptDetailModel) {
        return super.create(model, null);
    }
}

export { UaiPromptDetailRepository as api };
